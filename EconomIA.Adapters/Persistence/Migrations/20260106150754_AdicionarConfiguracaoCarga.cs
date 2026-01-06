using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EconomIA.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarConfiguracaoCarga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracao_carga",
                columns: table => new
                {
                    identificador = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    horario_execucao = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    dias_semana = table.Column<int[]>(type: "integer[]", nullable: false),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    dias_retroativos = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    dias_carga_inicial = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    max_concorrencia = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    carregar_compras = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    carregar_contratos = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    carregar_atas = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sincronizar_orgaos = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    horario_sincronizacao = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    dia_semana_sincronizacao = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_por = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracao_carga", x => x.identificador);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracao_carga");
        }
    }
}
