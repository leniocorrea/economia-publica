using System;
using EconomIA.Common.Domain;

namespace EconomIA.Domain;

public class ConfiguracaoCarga : Aggregate {
	protected ConfiguracaoCarga() {
	}

	public ConfiguracaoCarga(
		Int64 id,
		TimeOnly horarioExecucao,
		Int32[] diasSemana,
		Boolean habilitado,
		Int32 diasRetroativos,
		Int32 diasCargaInicial,
		Int32 maxConcorrencia,
		Boolean carregarCompras,
		Boolean carregarContratos,
		Boolean carregarAtas,
		Boolean sincronizarOrgaos,
		TimeOnly horarioSincronizacao,
		Int32 diaSemanasSincronizacao,
		DateTime atualizadoEm,
		String? atualizadoPor) : base(id) {
		HorarioExecucao = horarioExecucao;
		DiasSemana = diasSemana;
		Habilitado = habilitado;
		DiasRetroativos = diasRetroativos;
		DiasCargaInicial = diasCargaInicial;
		MaxConcorrencia = maxConcorrencia;
		CarregarCompras = carregarCompras;
		CarregarContratos = carregarContratos;
		CarregarAtas = carregarAtas;
		SincronizarOrgaos = sincronizarOrgaos;
		HorarioSincronizacao = horarioSincronizacao;
		DiaSemanasSincronizacao = diaSemanasSincronizacao;
		AtualizadoEm = atualizadoEm;
		AtualizadoPor = atualizadoPor;
	}

	public static ConfiguracaoCarga CriarPadrao() {
		return new ConfiguracaoCarga(
			id: 0,
			horarioExecucao: new TimeOnly(2, 0),
			diasSemana: [0, 1, 2, 3, 4, 5, 6],
			habilitado: true,
			diasRetroativos: 1,
			diasCargaInicial: 90,
			maxConcorrencia: 4,
			carregarCompras: true,
			carregarContratos: true,
			carregarAtas: true,
			sincronizarOrgaos: true,
			horarioSincronizacao: new TimeOnly(0, 0),
			diaSemanasSincronizacao: 0,
			atualizadoEm: DateTime.UtcNow,
			atualizadoPor: null
		);
	}

	public virtual TimeOnly HorarioExecucao { get; protected set; }
	public virtual Int32[] DiasSemana { get; protected set; } = [];
	public virtual Boolean Habilitado { get; protected set; }
	public virtual Int32 DiasRetroativos { get; protected set; }
	public virtual Int32 DiasCargaInicial { get; protected set; }
	public virtual Int32 MaxConcorrencia { get; protected set; }
	public virtual Boolean CarregarCompras { get; protected set; }
	public virtual Boolean CarregarContratos { get; protected set; }
	public virtual Boolean CarregarAtas { get; protected set; }
	public virtual Boolean SincronizarOrgaos { get; protected set; }
	public virtual TimeOnly HorarioSincronizacao { get; protected set; }
	public virtual Int32 DiaSemanasSincronizacao { get; protected set; }
	public virtual DateTime AtualizadoEm { get; protected set; }
	public virtual String? AtualizadoPor { get; protected set; }

	public void Atualizar(
		TimeOnly horarioExecucao,
		Int32[] diasSemana,
		Boolean habilitado,
		Int32 diasRetroativos,
		Int32 diasCargaInicial,
		Int32 maxConcorrencia,
		Boolean carregarCompras,
		Boolean carregarContratos,
		Boolean carregarAtas,
		Boolean sincronizarOrgaos,
		TimeOnly horarioSincronizacao,
		Int32 diaSemanasSincronizacao,
		String? atualizadoPor = null) {
		HorarioExecucao = horarioExecucao;
		DiasSemana = diasSemana;
		Habilitado = habilitado;
		DiasRetroativos = diasRetroativos;
		DiasCargaInicial = diasCargaInicial;
		MaxConcorrencia = maxConcorrencia;
		CarregarCompras = carregarCompras;
		CarregarContratos = carregarContratos;
		CarregarAtas = carregarAtas;
		SincronizarOrgaos = sincronizarOrgaos;
		HorarioSincronizacao = horarioSincronizacao;
		DiaSemanasSincronizacao = diaSemanasSincronizacao;
		AtualizadoEm = DateTime.UtcNow;
		AtualizadoPor = atualizadoPor;
	}
}
