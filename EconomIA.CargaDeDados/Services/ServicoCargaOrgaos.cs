using System.Diagnostics;
using System.Net.Http.Json;
using EconomIA.CargaDeDados.Dtos;
using EconomIA.CargaDeDados.Models;
using EconomIA.CargaDeDados.Repositories;

namespace EconomIA.CargaDeDados.Services;

public class ServicoCargaOrgaos {
	private readonly HttpClient httpClient;
	private readonly Orgaos orgaos;
	private readonly Unidades unidades;

	public ServicoCargaOrgaos(HttpClient httpClient, Orgaos orgaos, Unidades unidades) {
		this.httpClient = httpClient;
		this.orgaos = orgaos;
		this.unidades = unidades;
	}

	public async Task CarregarTodosOrgaosEUnidadesAsync(Int32 concorrenciaUnidades = 100) {
		var cronometro = Stopwatch.StartNew();

		Console.WriteLine("=== Carga Completa de Orgaos e Unidades do PNCP ===");
		Console.WriteLine();

		await CarregarTodosOrgaosAsync();
		await CarregarTodasUnidadesAsync(concorrenciaUnidades);

		cronometro.Stop();
		Console.WriteLine();
		Console.WriteLine($"=== Carga completa finalizada em {cronometro.Elapsed:hh\\:mm\\:ss} ===");
	}

	public async Task CarregarTodosOrgaosAsync() {
		var cronometro = Stopwatch.StartNew();

		Console.WriteLine("[1/2] Carregando orgaos do PNCP...");

		try {
			var url = "https://pncp.gov.br/pncp-api/v1/orgaos?pagina=1";
			Console.WriteLine($"  Requisitando: {url}");

			var listaOrgaosDto = await httpClient.GetFromJsonAsync<List<PncpOrgaoConsultaDto>>(url);

			if (listaOrgaosDto == null || listaOrgaosDto.Count == 0) {
				Console.WriteLine("  Nenhum orgao encontrado na API.");
				return;
			}

			Console.WriteLine($"  Recebidos {listaOrgaosDto.Count:N0} orgaos da API");

			var listaOrgaos = listaOrgaosDto.Select(dto => new Orgao {
				Cnpj = dto.Cnpj,
				RazaoSocial = dto.RazaoSocial,
				NomeFantasia = dto.NomeFantasia,
				CodigoNaturezaJuridica = dto.CodigoNaturezaJuridica,
				DescricaoNaturezaJuridica = dto.DescricaoNaturezaJuridica,
				PoderId = dto.PoderId,
				EsferaId = dto.EsferaId,
				SituacaoCadastral = dto.SituacaoCadastral,
				MotivoSituacaoCadastral = dto.MotivoSituacaoCadastral,
				DataSituacaoCadastral = dto.DataSituacaoCadastral,
				DataValidacao = dto.DataValidacao,
				Validado = dto.Validado,
				DataInclusaoPncp = dto.DataInclusao,
				DataAtualizacaoPncp = dto.DataAtualizacao,
				StatusAtivo = dto.StatusAtivo,
				JustificativaAtualizacao = dto.JustificativaAtualizacao
			}).ToList();

			var tamanhoLote = 1000;
			var totalLotes = (Int32)Math.Ceiling(listaOrgaos.Count / (Double)tamanhoLote);
			var inseridos = 0;

			Console.WriteLine($"  Inserindo em {totalLotes} lotes...");

			for (var i = 0; i < listaOrgaos.Count; i += tamanhoLote) {
				var lote = listaOrgaos.Skip(i).Take(tamanhoLote);
				await orgaos.UpsertEmLoteAsync(lote);
				inseridos += lote.Count();

				var loteAtual = (i / tamanhoLote) + 1;
				if (loteAtual % 10 == 0 || loteAtual == totalLotes) {
					Console.WriteLine($"  Lote {loteAtual}/{totalLotes} - {inseridos:N0} orgaos inseridos");
				}
			}

			cronometro.Stop();
			Console.WriteLine($"  {listaOrgaos.Count:N0} orgaos carregados em {cronometro.Elapsed.TotalSeconds:F1}s");

		} catch (Exception ex) {
			Console.WriteLine($"  ERRO: {ex.Message}");
			throw;
		}
	}

	public async Task CarregarTodasUnidadesAsync(Int32 maxConcorrencia = 100) {
		var cronometro = Stopwatch.StartNew();

		Console.WriteLine();
		Console.WriteLine("[2/2] Carregando unidades dos orgaos...");

		var orgaosSemUnidades = await orgaos.ListarOrgaosSemUnidadesAsync();
		var total = orgaosSemUnidades.Count;

		if (total == 0) {
			Console.WriteLine("  Todos os orgaos ja possuem unidades carregadas!");
			return;
		}

		Console.WriteLine($"  {total:N0} orgaos pendentes de unidades");

		var semaforo = new SemaphoreSlim(maxConcorrencia);
		var totalUnidades = 0;
		var processados = 0;
		var erros = 0;
		var orgaosSemUnidade = 0;

		var bufferUnidades = new System.Collections.Concurrent.ConcurrentBag<Unidade>();
		var tamanhoLote = 5000;
		var lockInsert = new SemaphoreSlim(1, 1);

		async Task InserirLoteSeNecessario(Boolean forcar = false) {
			if (bufferUnidades.Count < tamanhoLote && !forcar) {
				return;
			}

			await lockInsert.WaitAsync();
			try {
				if (bufferUnidades.Count >= tamanhoLote || forcar) {
					var lote = new List<Unidade>();
					while (bufferUnidades.TryTake(out var unidade)) {
						lote.Add(unidade);
					}

					if (lote.Count > 0) {
						await unidades.BulkUpsertAsync(lote);
					}
				}
			} finally {
				lockInsert.Release();
			}
		}

		var tarefas = orgaosSemUnidades.Select(async orgao => {
			await semaforo.WaitAsync();
			try {
				var unidadesObtidas = await ObterUnidadesDoOrgaoAsync(orgao.Cnpj, orgao.Identificador);

				if (unidadesObtidas.Count == 0) {
					Interlocked.Increment(ref orgaosSemUnidade);
				} else {
					Interlocked.Add(ref totalUnidades, unidadesObtidas.Count);
					foreach (var unidade in unidadesObtidas) {
						bufferUnidades.Add(unidade);
					}
					await InserirLoteSeNecessario();
				}

				var atual = Interlocked.Increment(ref processados);
				if (atual % 500 == 0 || atual == total) {
					var percentual = (atual * 100.0) / total;
					Console.WriteLine($"  [{atual:N0}/{total:N0}] {percentual:F1}% - {totalUnidades:N0} unidades");
				}
			} catch {
				Interlocked.Increment(ref erros);
			} finally {
				semaforo.Release();
			}
		}).ToArray();

		await Task.WhenAll(tarefas);

		await InserirLoteSeNecessario(forcar: true);

		cronometro.Stop();
		Console.WriteLine($"  {totalUnidades:N0} unidades carregadas em {cronometro.Elapsed:mm\\:ss}");
		Console.WriteLine($"  Orgaos processados: {processados:N0}");
		Console.WriteLine($"  Orgaos sem unidades: {orgaosSemUnidade:N0}");
		if (erros > 0) {
			Console.WriteLine($"  Erros: {erros}");
		}
	}

	private async Task<List<Unidade>> ObterUnidadesDoOrgaoAsync(String cnpj, Int64 identificadorOrgao) {
		try {
			var url = $"https://pncp.gov.br/pncp-api/v1/orgaos/{cnpj}/unidades";
			var listaUnidadesDto = await httpClient.GetFromJsonAsync<List<PncpUnidadeConsultaDto>>(url);

			if (listaUnidadesDto == null || listaUnidadesDto.Count == 0) {
				return new List<Unidade>();
			}

			return listaUnidadesDto.Select(dto => new Unidade {
				IdentificadorDoOrgao = identificadorOrgao,
				CodigoUnidade = dto.CodigoUnidade,
				NomeUnidade = dto.NomeUnidade,
				MunicipioNome = dto.Municipio?.Nome,
				MunicipioCodigoIbge = dto.Municipio?.CodigoIbge,
				UfSigla = dto.Municipio?.Uf?.SiglaUf,
				UfNome = dto.Municipio?.Uf?.NomeUf,
				StatusAtivo = dto.StatusAtivo,
				DataInclusaoPncp = dto.DataInclusao,
				DataAtualizacaoPncp = dto.DataAtualizacao,
				JustificativaAtualizacao = dto.JustificativaAtualizacao
			}).ToList();
		} catch {
			return new List<Unidade>();
		}
	}

	private async Task<Int32> CarregarUnidadesDoOrgaoAsync(String cnpj, Int64 identificadorOrgao) {
		try {
			var url = $"https://pncp.gov.br/pncp-api/v1/orgaos/{cnpj}/unidades";
			var listaUnidadesDto = await httpClient.GetFromJsonAsync<List<PncpUnidadeConsultaDto>>(url);

			if (listaUnidadesDto == null || listaUnidadesDto.Count == 0) {
				return 0;
			}

			var listaUnidades = listaUnidadesDto.Select(dto => new Unidade {
				IdentificadorDoOrgao = identificadorOrgao,
				CodigoUnidade = dto.CodigoUnidade,
				NomeUnidade = dto.NomeUnidade,
				MunicipioNome = dto.Municipio?.Nome,
				MunicipioCodigoIbge = dto.Municipio?.CodigoIbge,
				UfSigla = dto.Municipio?.Uf?.SiglaUf,
				UfNome = dto.Municipio?.Uf?.NomeUf,
				StatusAtivo = dto.StatusAtivo,
				DataInclusaoPncp = dto.DataInclusao,
				DataAtualizacaoPncp = dto.DataAtualizacao,
				JustificativaAtualizacao = dto.JustificativaAtualizacao
			}).ToList();

			await unidades.UpsertEmLoteAsync(listaUnidades);
			return listaUnidades.Count;
		} catch {
			return 0;
		}
	}

	public async Task<Boolean> ImportarOrgaoPorCnpjAsync(String cnpj) {
		try {
			var url = $"https://pncp.gov.br/pncp-api/v1/orgaos/{cnpj}";
			var orgaoDto = await httpClient.GetFromJsonAsync<PncpOrgaoConsultaDto>(url);

			if (orgaoDto == null) {
				Console.WriteLine($"Orgao nao encontrado: {cnpj}");
				return false;
			}

			var orgao = new Orgao {
				Cnpj = orgaoDto.Cnpj,
				RazaoSocial = orgaoDto.RazaoSocial,
				NomeFantasia = orgaoDto.NomeFantasia,
				CodigoNaturezaJuridica = orgaoDto.CodigoNaturezaJuridica,
				DescricaoNaturezaJuridica = orgaoDto.DescricaoNaturezaJuridica,
				PoderId = orgaoDto.PoderId,
				EsferaId = orgaoDto.EsferaId,
				SituacaoCadastral = orgaoDto.SituacaoCadastral,
				MotivoSituacaoCadastral = orgaoDto.MotivoSituacaoCadastral,
				DataSituacaoCadastral = orgaoDto.DataSituacaoCadastral,
				DataValidacao = orgaoDto.DataValidacao,
				Validado = orgaoDto.Validado,
				DataInclusaoPncp = orgaoDto.DataInclusao,
				DataAtualizacaoPncp = orgaoDto.DataAtualizacao,
				StatusAtivo = orgaoDto.StatusAtivo,
				JustificativaAtualizacao = orgaoDto.JustificativaAtualizacao
			};

			var identificadorOrgao = await orgaos.UpsertAsync(orgao);
			Console.WriteLine($"Orgao importado: {orgaoDto.RazaoSocial} (ID: {identificadorOrgao})");

			var unidadesCarregadas = await CarregarUnidadesDoOrgaoAsync(cnpj, identificadorOrgao);
			Console.WriteLine($"Unidades carregadas: {unidadesCarregadas}");

			return true;
		} catch (Exception ex) {
			Console.WriteLine($"Erro ao importar orgao {cnpj}: {ex.Message}");
			return false;
		}
	}
}
