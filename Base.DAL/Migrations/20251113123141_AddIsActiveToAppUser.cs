using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClincAdminProfiles_AspNetUsers_UserId",
                table: "ClincAdminProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ClincDoctorProfiles_AspNetUsers_UserId",
                table: "ClincDoctorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ClincReceptionistProfiles_AspNetUsers_UserId",
                table: "ClincReceptionistProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAdminProfile_AspNetUsers_UserId",
                table: "SystemAdminProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserId",
                table: "UserProfiles");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ClincAdminProfiles_AspNetUsers_UserId",
                table: "ClincAdminProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClincDoctorProfiles_AspNetUsers_UserId",
                table: "ClincDoctorProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClincReceptionistProfiles_AspNetUsers_UserId",
                table: "ClincReceptionistProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAdminProfile_AspNetUsers_UserId",
                table: "SystemAdminProfile",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserId",
                table: "UserProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClincAdminProfiles_AspNetUsers_UserId",
                table: "ClincAdminProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ClincDoctorProfiles_AspNetUsers_UserId",
                table: "ClincDoctorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ClincReceptionistProfiles_AspNetUsers_UserId",
                table: "ClincReceptionistProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemAdminProfile_AspNetUsers_UserId",
                table: "SystemAdminProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_ClincAdminProfiles_AspNetUsers_UserId",
                table: "ClincAdminProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClincDoctorProfiles_AspNetUsers_UserId",
                table: "ClincDoctorProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClincReceptionistProfiles_AspNetUsers_UserId",
                table: "ClincReceptionistProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemAdminProfile_AspNetUsers_UserId",
                table: "SystemAdminProfile",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserId",
                table: "UserProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
