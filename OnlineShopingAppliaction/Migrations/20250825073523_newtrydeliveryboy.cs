using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShopingAppliaction.Migrations
{
    /// <inheritdoc />
    public partial class newtrydeliveryboy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShippingFullName",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryBoyId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryBoyId",
                table: "Orders",
                column: "DeliveryBoyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AppUsers_DeliveryBoyId",
                table: "Orders",
                column: "DeliveryBoyId",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AppUsers_DeliveryBoyId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryBoyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryBoyId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "ShippingFullName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
