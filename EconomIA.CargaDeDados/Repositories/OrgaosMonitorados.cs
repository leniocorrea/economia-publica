using System.Data;
using Dapper;
using EconomIA.CargaDeDados.Models;

namespace EconomIA.CargaDeDados.Repositories;

public class OrgaosMonitorados {
	private readonly IDbConnection conexao;

	public OrgaosMonitorados(IDbConnection conexao) {
		this.conexao = conexao;
	}

	public async Task<List<OrgaoResumo>> ListarAtivosAsync() {
		var sql = @"
			SELECT
				o.identificador AS Identificador,
				o.cnpj AS Cnpj,
				o.razao_social AS RazaoSocial
			FROM public.orgao_monitorado om
			INNER JOIN public.orgao o ON o.identificador = om.identificador_do_orgao
			WHERE om.ativo = true
			ORDER BY o.razao_social;
		";

		var resultado = await conexao.QueryAsync<OrgaoResumo>(sql);
		return resultado.ToList();
	}

	public async Task<List<OrgaoResumo>> ListarPorCnpjsAsync(String[] cnpjs) {
		var sql = @"
			SELECT
				o.identificador AS Identificador,
				o.cnpj AS Cnpj,
				o.razao_social AS RazaoSocial
			FROM public.orgao_monitorado om
			INNER JOIN public.orgao o ON o.identificador = om.identificador_do_orgao
			WHERE om.ativo = true
			AND o.cnpj = ANY(@Cnpjs)
			ORDER BY o.razao_social;
		";

		var resultado = await conexao.QueryAsync<OrgaoResumo>(sql, new { Cnpjs = cnpjs });
		return resultado.ToList();
	}

	public async Task<Boolean> OrgaoEstaMonitoradoAsync(Int64 identificadorDoOrgao) {
		var sql = @"
			SELECT EXISTS (
				SELECT 1 FROM public.orgao_monitorado
				WHERE identificador_do_orgao = @IdentificadorDoOrgao
				AND ativo = true
			);
		";

		return await conexao.ExecuteScalarAsync<Boolean>(sql, new { IdentificadorDoOrgao = identificadorDoOrgao });
	}

	public async Task<Int32> ContarAtivosAsync() {
		var sql = "SELECT COUNT(*) FROM public.orgao_monitorado WHERE ativo = true;";
		return await conexao.ExecuteScalarAsync<Int32>(sql);
	}
}
