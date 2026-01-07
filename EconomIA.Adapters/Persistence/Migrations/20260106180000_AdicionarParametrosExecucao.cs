using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EconomIA.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarParametrosExecucao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parametros",
                table: "execucao_carga",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<System.DateTime>(
                name: "inicio_em",
                table: "execucao_carga",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(System.DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.Sql(@"
                ALTER TABLE execucao_carga DROP CONSTRAINT IF EXISTS chk_status_execucao;
                ALTER TABLE execucao_carga ADD CONSTRAINT chk_status_execucao
                    CHECK (status IN ('pendente', 'em_andamento', 'sucesso', 'erro', 'cancelado', 'parcial'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE execucao_carga DROP CONSTRAINT IF EXISTS chk_status_execucao;
                ALTER TABLE execucao_carga ADD CONSTRAINT chk_status_execucao
                    CHECK (status IN ('em_andamento', 'sucesso', 'erro', 'cancelado', 'parcial'));
            ");

            migrationBuilder.AlterColumn<System.DateTime>(
                name: "inicio_em",
                table: "execucao_carga",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new System.DateTime(1, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Unspecified),
                oldClrType: typeof(System.DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "parametros",
                table: "execucao_carga");
        }
    }
}
