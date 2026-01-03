using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EconomIA.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaOrgaoMonitorado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orgao_monitorado",
                columns: table => new
                {
                    identificador = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    identificador_do_orgao = table.Column<long>(type: "bigint", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orgao_monitorado", x => x.identificador);
                    table.ForeignKey(
                        name: "fk_orgao_monitorado_identificador_do_orgao_to_orgao_identificador",
                        column: x => x.identificador_do_orgao,
                        principalTable: "orgao",
                        principalColumn: "identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "un_orgao_monitorado_identificador_do_orgao",
                table: "orgao_monitorado",
                column: "identificador_do_orgao",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orgao_monitorado");
        }
    }
}
