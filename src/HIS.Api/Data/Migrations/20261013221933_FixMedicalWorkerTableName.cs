using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HIS.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixMedicalWorkerTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecordHistory_MedicalWorker_MedicalWorkerId",
                table: "MedicalRecordHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicalWorker",
                table: "MedicalWorker");

            migrationBuilder.RenameTable(
                name: "MedicalWorker",
                newName: "MedicalWorkers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicalWorkers",
                table: "MedicalWorkers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecordHistory_MedicalWorkers_MedicalWorkerId",
                table: "MedicalRecordHistory",
                column: "MedicalWorkerId",
                principalTable: "MedicalWorkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecordHistory_MedicalWorkers_MedicalWorkerId",
                table: "MedicalRecordHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicalWorkers",
                table: "MedicalWorkers");

            migrationBuilder.RenameTable(
                name: "MedicalWorkers",
                newName: "MedicalWorker");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicalWorker",
                table: "MedicalWorker",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecordHistory_MedicalWorker_MedicalWorkerId",
                table: "MedicalRecordHistory",
                column: "MedicalWorkerId",
                principalTable: "MedicalWorker",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
