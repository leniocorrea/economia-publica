using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class Orgaos {
	private readonly IDbConnection conexao;

	public Orgaos(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task<long> UpsertAsync(Orgao orgao) {
		var sql = @"
			insert into public.orgao (
				cnpj, razao_social, nome_fantasia, codigo_natureza_juridica, descricao_natureza_juridica,
				poder_id, esfera_id, situacao_cadastral, motivo_situacao_cadastral, data_situacao_cadastral,
				data_validacao, validado, data_inclusao_pncp, data_atualizacao_pncp, status_ativo,
				justificativa_atualizacao, atualizado_em
			)
			values (
				@Cnpj, @RazaoSocial, @NomeFantasia, @CodigoNaturezaJuridica, @DescricaoNaturezaJuridica,
				@PoderId, @EsferaId, @SituacaoCadastral, @MotivoSituacaoCadastral, @DataSituacaoCadastral,
				@DataValidacao, @Validado, @DataInclusaoPncp, @DataAtualizacaoPncp, @StatusAtivo,
				@JustificativaAtualizacao, now()
			)
			on conflict (cnpj) do update
			set razao_social = excluded.razao_social,
				nome_fantasia = excluded.nome_fantasia,
				codigo_natureza_juridica = excluded.codigo_natureza_juridica,
				descricao_natureza_juridica = excluded.descricao_natureza_juridica,
				poder_id = excluded.poder_id,
				esfera_id = excluded.esfera_id,
				situacao_cadastral = excluded.situacao_cadastral,
				motivo_situacao_cadastral = excluded.motivo_situacao_cadastral,
				data_situacao_cadastral = excluded.data_situacao_cadastral,
				data_validacao = excluded.data_validacao,
				validado = excluded.validado,
				data_inclusao_pncp = excluded.data_inclusao_pncp,
				data_atualizacao_pncp = excluded.data_atualizacao_pncp,
				status_ativo = excluded.status_ativo,
				justificativa_atualizacao = excluded.justificativa_atualizacao,
				atualizado_em = now()
			returning identificador;
		";

		return await conexao.ExecuteScalarAsync<long>(sql, orgao);
	}

	public async Task<List<string>> ListarCnpjsAsync() {
		var sql = "select cnpj from public.orgao order by razao_social";
		var resultado = await conexao.QueryAsync<string>(sql);
		return resultado.ToList();
	}

	public async Task<List<OrgaoResumo>> ListarOrgaosSemUnidadesAsync() {
		var sql = @"
			select o.identificador, o.cnpj, o.razao_social as RazaoSocial
			from public.orgao o
			where not exists (
				select 1 from public.unidade u
				where u.identificador_do_orgao = o.identificador
			)
			order by o.razao_social";
		var resultado = await conexao.QueryAsync<OrgaoResumo>(sql);
		return resultado.ToList();
	}

	public async Task<List<OrgaoResumo>> ListarTodosAsync() {
		var sql = @"
			select identificador, cnpj, razao_social as RazaoSocial
			from public.orgao
			order by razao_social";
		var resultado = await conexao.QueryAsync<OrgaoResumo>(sql);
		return resultado.ToList();
	}

	public async Task<List<OrgaoResumo>> ListarPorCnpjsAsync(string[] cnpjs) {
		var sql = @"
			select identificador, cnpj, razao_social as RazaoSocial
			from public.orgao
			where cnpj = ANY(@Cnpjs)
			order by razao_social";
		var resultado = await conexao.QueryAsync<OrgaoResumo>(sql, new { Cnpjs = cnpjs });
		return resultado.ToList();
	}

	public async Task<OrgaoResumo?> ObterPorCnpjAsync(string cnpj) {
		var sql = @"
			select identificador, cnpj, razao_social as RazaoSocial
			from public.orgao
			where cnpj = @Cnpj";
		return await conexao.QueryFirstOrDefaultAsync<OrgaoResumo>(sql, new { Cnpj = cnpj });
	}

	public async Task<int> UpsertEmLoteAsync(IEnumerable<Orgao> listaOrgaos) {
		var sql = @"
			insert into public.orgao (
				cnpj, razao_social, nome_fantasia, codigo_natureza_juridica, descricao_natureza_juridica,
				poder_id, esfera_id, situacao_cadastral, motivo_situacao_cadastral, data_situacao_cadastral,
				data_validacao, validado, data_inclusao_pncp, data_atualizacao_pncp, status_ativo,
				justificativa_atualizacao, atualizado_em
			)
			values (
				@Cnpj, @RazaoSocial, @NomeFantasia, @CodigoNaturezaJuridica, @DescricaoNaturezaJuridica,
				@PoderId, @EsferaId, @SituacaoCadastral, @MotivoSituacaoCadastral, @DataSituacaoCadastral,
				@DataValidacao, @Validado, @DataInclusaoPncp, @DataAtualizacaoPncp, @StatusAtivo,
				@JustificativaAtualizacao, now()
			)
			on conflict (cnpj) do update
			set razao_social = excluded.razao_social,
				nome_fantasia = excluded.nome_fantasia,
				codigo_natureza_juridica = excluded.codigo_natureza_juridica,
				descricao_natureza_juridica = excluded.descricao_natureza_juridica,
				poder_id = excluded.poder_id,
				esfera_id = excluded.esfera_id,
				situacao_cadastral = excluded.situacao_cadastral,
				motivo_situacao_cadastral = excluded.motivo_situacao_cadastral,
				data_situacao_cadastral = excluded.data_situacao_cadastral,
				data_validacao = excluded.data_validacao,
				validado = excluded.validado,
				data_inclusao_pncp = excluded.data_inclusao_pncp,
				data_atualizacao_pncp = excluded.data_atualizacao_pncp,
				status_ativo = excluded.status_ativo,
				justificativa_atualizacao = excluded.justificativa_atualizacao,
				atualizado_em = now();
		";

		return await conexao.ExecuteAsync(sql, listaOrgaos);
	}

	public async Task<Dictionary<string, long>> ObterMapaCnpjIdentificadorAsync() {
		var sql = "select cnpj, identificador from public.orgao";
		var resultado = await conexao.QueryAsync<(string Cnpj, long Identificador)>(sql);
		return resultado.ToDictionary(x => x.Cnpj, x => x.Identificador);
	}
}

public class OrgaoResumo {
	public long Identificador { get; set; }
	public string Cnpj { get; set; } = string.Empty;
	public string RazaoSocial { get; set; } = string.Empty;
}
