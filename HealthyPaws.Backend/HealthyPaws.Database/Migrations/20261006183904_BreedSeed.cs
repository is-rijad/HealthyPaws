using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HealthyPaws.Database.Migrations
{
    /// <inheritdoc />
    public partial class BreedSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Breeds",
                columns: new[] { "Id", "MaxWeight", "MinWeight", "Name" },
                values: new object[,]
                {
                    { 1, 4.54m, 3.18m, "Afenpinčer" },
                    { 2, 27.22m, 22.68m, "Afganistanski hrt" },
                    { 3, 31.75m, 22.68m, "Erdel terijer" },
                    { 4, 58.97m, 31.75m, "Akita" },
                    { 5, 38.56m, 34.02m, "Aljaški malamut" },
                    { 6, 45.36m, 27.22m, "Američki buldog" },
                    { 7, 29.48m, 20.41m, "Američki engleski kunhaund" },
                    { 8, 15.88m, 2.72m, "Američki eskimski pas" },
                    { 9, 31.75m, 27.22m, "Američki foksaund" },
                    { 10, 7.26m, 5.44m, "Američki golokožni terijer" },
                    { 11, 31.75m, 20.41m, "Američki leopard gonič" },
                    { 12, 31.75m, 18.14m, "Američki stafordširski terijer" },
                    { 13, 20.41m, 11.34m, "Američki vodeni španijel" },
                    { 14, 68.04m, 36.29m, "Anadolski ovčar" },
                    { 15, 31.75m, 21.77m, "Apenzelski planinski pas" },
                    { 16, 22.68m, 15.88m, "Australski pastirski pas" },
                    { 17, 20.41m, 11.34m, "Australski kelpi" },
                    { 18, 29.48m, 18.14m, "Australski ovčar" },
                    { 19, 20.41m, 14.51m, "Australski pastirski pas kratkog repa" },
                    { 20, 9.07m, 6.80m, "Australski terijer" },
                    { 21, 24.95m, 14.97m, "Azavak" },
                    { 22, 29.48m, 15.88m, "Barbet" },
                    { 23, 10.89m, 9.98m, "Basenži" },
                    { 24, 15.88m, 12.25m, "Baset fov de Bretanj" },
                    { 25, 29.48m, 18.14m, "Baset" },
                    { 26, 29.94m, 16.78m, "Bavarski planinski gonič" },
                    { 27, 13.61m, 9.07m, "Bigl" },
                    { 28, 24.95m, 20.41m, "Bradati koli" },
                    { 29, 49.90m, 31.75m, "Boseron" },
                    { 30, 10.43m, 7.71m, "Bedlington terijer" },
                    { 31, 29.48m, 24.95m, "Belgijski ovčar lakenoa" },
                    { 32, 36.29m, 18.14m, "Belgijski ovčar malinoa" },
                    { 33, 34.02m, 20.41m, "Belgijski ovčar grunendal" },
                    { 34, 34.02m, 20.41m, "Belgijski ovčar tervirenac" },
                    { 35, 38.10m, 25.85m, "Bergamaski ovčar" },
                    { 36, 31.75m, 22.68m, "Pikardijski ovčar" },
                    { 37, 52.16m, 31.75m, "Bernski planinski pas" },
                    { 38, 8.16m, 5.44m, "Bišon friže" },
                    { 39, 3.63m, 1.81m, "Biver terijer" },
                    { 40, 49.90m, 29.48m, "Crno-smeđi kunhaund" },
                    { 41, 58.97m, 36.29m, "Crni ruski terijer" },
                    { 42, 49.90m, 36.29m, "Bladhaund" },
                    { 43, 36.29m, 20.41m, "Bluetik kunhaund" },
                    { 44, 90.72m, 68.04m, "Burbul" },
                    { 45, 27.22m, 16.78m, "Češki ovčar" },
                    { 46, 4.08m, 2.49m, "Bolonjez" },
                    { 47, 24.95m, 13.61m, "Border koli" },
                    { 48, 7.03m, 5.22m, "Border terijer" },
                    { 49, 47.63m, 27.22m, "Borzoj" },
                    { 50, 11.34m, 5.44m, "Bostonski terijer" },
                    { 51, 49.90m, 31.75m, "Flandrijski govedarski pas" },
                    { 52, 36.29m, 22.68m, "Bokser" },
                    { 53, 18.14m, 11.34m, "Bojkin španijel" },
                    { 54, 40.82m, 24.95m, "Italijanski brako" },
                    { 55, 24.04m, 15.88m, "Burbonski brak" },
                    { 56, 24.95m, 18.14m, "Francuski pirinejski brak" },
                    { 57, 45.36m, 24.95m, "Briar" },
                    { 58, 18.14m, 13.61m, "Bretanjski španijel" },
                    { 59, 68.04m, 40.82m, "Broholmer" },
                    { 60, 4.54m, 3.63m, "Briselski grifon" },
                    { 61, 31.75m, 22.68m, "Bul terijer" },
                    { 62, 22.68m, 18.14m, "Engleski buldog" },
                    { 63, 58.97m, 45.36m, "Bulmastif" },
                    { 64, 6.35m, 5.90m, "Kern terijer" },
                    { 65, 24.95m, 15.88m, "Kanaanski pas" },
                    { 66, 49.90m, 40.82m, "Kane korso" },
                    { 67, 17.24m, 13.61m, "Kardigan velški korgi" },
                    { 68, 24.95m, 13.61m, "Karolinski pas" },
                    { 69, 43.09m, 22.68m, "Katahula leopard pas" },
                    { 70, 77.11m, 44.91m, "Kavkaski ovčar" },
                    { 71, 8.16m, 5.90m, "Kavalir king čarls španijel" },
                    { 72, 49.90m, 39.92m, "Srednjoazijski ovčar" },
                    { 73, 10.89m, 6.35m, "Češki terijer" },
                    { 74, 36.29m, 24.95m, "Česapik bej retriver" },
                    { 75, 2.72m, 1.36m, "Čivava" },
                    { 76, 5.44m, 3.63m, "Kineski goli pas" },
                    { 77, 27.22m, 20.41m, "Šar pej" },
                    { 78, 40.82m, 22.68m, "Činuk" },
                    { 79, 31.75m, 20.41m, "Čau čau" },
                    { 80, 11.79m, 7.71m, "Čirneko dele Etna" },
                    { 81, 38.56m, 24.95m, "Klamber španijel" },
                    { 82, 13.61m, 9.07m, "Američki koker španijel" },
                    { 83, 34.02m, 22.68m, "Koli" },
                    { 84, 6.80m, 3.63m, "Koton de Tulear" },
                    { 85, 19.96m, 13.15m, "Hrvatski ovčar" },
                    { 86, 43.09m, 27.22m, "Kovrdžavi retriver" },
                    { 87, 25.85m, 19.96m, "Čehoslovački vučjak" },
                    { 88, 14.51m, 4.99m, "Jazavčar" },
                    { 89, 31.75m, 20.41m, "Dalmatinac" },
                    { 90, 10.89m, 8.16m, "Dendi dinmont terijer" },
                    { 91, 9.07m, 6.80m, "Dansko-švedski pas farmer" },
                    { 92, 24.95m, 18.14m, "Njemački prepeličar" },
                    { 93, 45.36m, 27.22m, "Doberman" },
                    { 94, 45.36m, 39.92m, "Argentinska doga" },
                    { 95, 49.90m, 44.91m, "Bordoška doga" },
                    { 96, 33.11m, 21.77m, "Drentski ptičar" },
                    { 97, 18.14m, 15.88m, "Dreber" },
                    { 98, 34.02m, 19.05m, "Holandski ovčar" },
                    { 99, 15.42m, 11.79m, "Engleski koker španijel" },
                    { 100, 34.02m, 27.22m, "Engleski foksaund" },
                    { 101, 36.29m, 20.41m, "Engleski seter" },
                    { 102, 22.68m, 18.14m, "Engleski springer španijel" },
                    { 103, 6.35m, 3.63m, "Engleski patuljasti španijel" },
                    { 104, 29.48m, 18.14m, "Entlebuški planinski pas" },
                    { 105, 59.87m, 34.93m, "Estrelski planinski pas" },
                    { 106, 31.75m, 18.14m, "Euroazijac" },
                    { 107, 22.68m, 15.88m, "Filed španijel" },
                    { 108, 24.04m, 14.97m, "Finski laponski pas" },
                    { 109, 14.97m, 9.07m, "Finski špic" },
                    { 110, 31.75m, 27.22m, "Ravnodlaki retriver" },
                    { 111, 12.70m, 7.26m, "Francuski buldog" },
                    { 112, 27.22m, 22.68m, "Francuski španijel" },
                    { 113, 36.29m, 24.95m, "Njemački dugodlaki ptičar" },
                    { 114, 20.41m, 11.34m, "Njemački pinč" },
                    { 115, 40.82m, 22.68m, "Njemački ovčar" },
                    { 116, 31.75m, 20.41m, "Njemački kratkodlaki ptičar" },
                    { 117, 11.79m, 10.89m, "Njemački špic" },
                    { 118, 31.75m, 22.68m, "Njemački oštrodlaki ptičar" },
                    { 119, 38.56m, 24.95m, "Džinovski šnaucer" },
                    { 120, 18.14m, 14.51m, "Glen of Imal terijer" },
                    { 121, 34.02m, 24.95m, "Zlatni retriver" },
                    { 122, 36.29m, 20.41m, "Gordon seter" },
                    { 123, 20.41m, 18.14m, "Veliki vandejski baset grifon" },
                    { 124, 79.38m, 49.90m, "Njemačka doga" },
                    { 125, 45.36m, 38.56m, "Pirinejski planinski pas" },
                    { 126, 63.50m, 38.56m, "Veliki švicarski planinski pas" },
                    { 127, 31.75m, 27.22m, "Engleski hrt" },
                    { 128, 34.02m, 18.14m, "Hamiltonstovare" },
                    { 129, 44.91m, 35.83m, "Hanoverski gonič" },
                    { 130, 27.22m, 20.41m, "Harijer" },
                    { 131, 5.90m, 3.18m, "Havanski bišon" },
                    { 132, 29.94m, 19.96m, "Hokaido" },
                    { 133, 40.82m, 29.48m, "Hovavart" },
                    { 134, 22.68m, 20.41m, "Ibicanski hrt" },
                    { 135, 13.61m, 11.34m, "Islandski ovčar" },
                    { 136, 27.22m, 15.88m, "Irski crveno-bijeli seter" },
                    { 137, 31.75m, 27.22m, "Irski seter" },
                    { 138, 12.25m, 11.34m, "Irski terijer" },
                    { 139, 30.84m, 20.41m, "Irski vodeni španijel" },
                    { 140, 54.43m, 47.63m, "Irski vučjak" },
                    { 141, 6.35m, 3.18m, "Italijanski hrt" },
                    { 142, 9.98m, 7.71m, "Njemački lovni terijer" },
                    { 143, 4.99m, 3.18m, "Japanski čin" },
                    { 144, 11.34m, 4.54m, "Japanski špic" },
                    { 145, 22.68m, 13.61m, "Jindo" },
                    { 146, 18.14m, 9.07m, "Kai ken" },
                    { 147, 22.23m, 19.96m, "Karelijski medvjeđi pas" },
                    { 148, 20.41m, 15.88m, "Kishond" },
                    { 149, 18.14m, 14.97m, "Keri blu terijer" },
                    { 150, 27.22m, 13.61m, "Kišu ken" },
                    { 151, 45.36m, 36.29m, "Komondor" },
                    { 152, 15.88m, 9.07m, "Kromforlender" },
                    { 153, 52.16m, 31.75m, "Kuvas" },
                    { 154, 36.29m, 24.95m, "Labrador retriver" },
                    { 155, 15.88m, 10.89m, "Lagoto romanjolo" },
                    { 156, 7.71m, 6.80m, "Lejkland terijer" },
                    { 157, 7.71m, 4.08m, "Lankašir hiler" },
                    { 158, 29.94m, 19.96m, "Laponski pastirski pas" },
                    { 159, 77.11m, 40.82m, "Leonberger" },
                    { 160, 8.16m, 5.44m, "Lasa apso" },
                    { 161, 6.80m, 4.54m, "Levčen" },
                    { 162, 3.18m, 1.81m, "Maltezer" },
                    { 163, 9.98m, 5.44m, "Mančester terijer (standardni)" },
                    { 164, 5.44m, 2.72m, "Mančester terijer (patuljasti)" },
                    { 165, 104.33m, 54.43m, "Mastif" },
                    { 166, 18.14m, 9.07m, "Patuljasti američki ovčar" },
                    { 167, 12.70m, 8.16m, "Patuljasti bul terijer" },
                    { 168, 4.54m, 3.63m, "Patuljasti pinč" },
                    { 169, 9.07m, 4.99m, "Patuljasti šnaucer" },
                    { 170, 27.22m, 13.61m, "Planinski kur" },
                    { 171, 13.15m, 8.16m, "Mudi" },
                    { 172, 68.04m, 49.90m, "Napuljski mastif" },
                    { 173, 13.61m, 9.07m, "Holandski kojkerhondje" },
                    { 174, 68.04m, 45.36m, "Njufaundlend" },
                    { 175, 5.44m, 4.99m, "Norfolk terijer" },
                    { 176, 13.61m, 9.07m, "Norbotenspic" },
                    { 177, 18.14m, 11.79m, "Norveški buhund" },
                    { 178, 24.95m, 21.77m, "Norveški losov pas" },
                    { 179, 13.61m, 9.07m, "Norveški lundehund" },
                    { 180, 5.44m, 4.99m, "Norvič terijer" },
                    { 181, 22.68m, 15.88m, "Retriver za mamljenje pataka iz Nove Škotske" },
                    { 182, 45.36m, 27.22m, "Staroengleski ovčar" },
                    { 183, 52.16m, 36.29m, "Otahaund" },
                    { 184, 4.54m, 2.27m, "Papijon" },
                    { 185, 7.71m, 5.90m, "Parson rasel terijer" },
                    { 186, 6.35m, 3.18m, "Pekinezer" },
                    { 187, 13.61m, 11.34m, "Pembrok velški korgi" },
                    { 188, 49.90m, 38.10m, "Kanarska doga" },
                    { 189, 24.95m, 3.86m, "Peruanski goli pas" },
                    { 190, 18.14m, 11.34m, "Mali vandejski baset grifon" },
                    { 191, 24.95m, 20.41m, "Faraonski pas" },
                    { 192, 27.22m, 18.14m, "Plot gonič" },
                    { 193, 34.02m, 20.41m, "Pointer" },
                    { 194, 22.68m, 13.61m, "Poljski nizinski ovčar" },
                    { 195, 3.18m, 1.36m, "Pomeranac" },
                    { 196, 6.80m, 4.54m, "Pudla (patuljasta)" },
                    { 197, 31.75m, 18.14m, "Pudla (standardna)" },
                    { 198, 2.72m, 1.81m, "Pudla (toy)" },
                    { 199, 28.12m, 24.95m, "Porselen" },
                    { 200, 29.94m, 15.88m, "Portugalski podengo" },
                    { 201, 5.90m, 4.08m, "Portugalski podengo pekeno" },
                    { 202, 26.76m, 15.88m, "Portugalski ptičar" },
                    { 203, 26.76m, 17.01m, "Portugalski ovčar" },
                    { 204, 27.22m, 15.88m, "Portugalski vodeni pas" },
                    { 205, 31.75m, 20.41m, "Pudlpointer" },
                    { 206, 8.16m, 6.35m, "Mops" },
                    { 207, 15.88m, 11.34m, "Puli" },
                    { 208, 13.15m, 9.98m, "Pumi" },
                    { 209, 108.86m, 54.43m, "Pirinejski mastif" },
                    { 210, 13.61m, 6.80m, "Pirinejski ovčar" },
                    { 211, 59.87m, 34.93m, "Rafeiro do Alentejo" },
                    { 212, 11.34m, 4.54m, "Pacovski terijer" },
                    { 213, 31.75m, 20.41m, "Redbon kunhaund" },
                    { 214, 38.56m, 31.75m, "Rodezijski ridžbek" },
                    { 215, 58.97m, 45.36m, "Rumunski mioritski ovčar" },
                    { 216, 61.23m, 36.29m, "Rotvajler" },
                    { 217, 6.80m, 4.08m, "Rasel terijer" },
                    { 218, 2.95m, 1.36m, "Ruski toy terijer" },
                    { 219, 4.99m, 2.04m, "Ruski cvjetni bolonjez" },
                    { 220, 81.65m, 54.43m, "Bernardinac" },
                    { 221, 29.48m, 18.14m, "Saluki" },
                    { 222, 29.48m, 15.88m, "Samojed" },
                    { 223, 24.95m, 11.79m, "Šapendes" },
                    { 224, 7.26m, 4.54m, "Šiperke" },
                    { 225, 49.90m, 34.02m, "Škotski jelenski hrt" },
                    { 226, 9.98m, 8.16m, "Škotski terijer" },
                    { 227, 10.89m, 10.43m, "Silihem terijer" },
                    { 228, 27.22m, 18.14m, "Italijanski gonič" },
                    { 229, 11.34m, 6.80m, "Šetlandski ovčar" },
                    { 230, 10.43m, 7.71m, "Šiba ino" },
                    { 231, 7.26m, 4.08m, "Ši cu" },
                    { 232, 24.95m, 15.88m, "Šikoku" },
                    { 233, 27.22m, 15.88m, "Sibirski haski" },
                    { 234, 4.54m, 3.63m, "Svilenkasti terijer" },
                    { 235, 20.41m, 15.88m, "Skaj terijer" },
                    { 236, 22.68m, 15.88m, "Slugi" },
                    { 237, 29.48m, 22.68m, "Slovački oštrodlaki ptičar" },
                    { 238, 44.00m, 30.84m, "Slovački čuvač" },
                    { 239, 19.96m, 14.97m, "Slovački gonič" },
                    { 240, 27.22m, 18.14m, "Mali minsterlender" },
                    { 241, 8.16m, 6.80m, "Glatkodlaki foksterijer" },
                    { 242, 18.14m, 13.61m, "Mekodlaki pšenični terijer" },
                    { 243, 90.72m, 63.50m, "Španski mastif" },
                    { 244, 22.23m, 14.06m, "Španski vodeni pas" },
                    { 245, 39.01m, 27.67m, "Italijanski spinone" },
                    { 246, 27.22m, 18.14m, "Stabijhun" },
                    { 247, 17.24m, 10.89m, "Stafordširski bul terijer" },
                    { 248, 22.68m, 13.61m, "Srednji šnaucer" },
                    { 249, 20.41m, 15.88m, "Saseks španijel" },
                    { 250, 20.41m, 13.61m, "Švedski laponski pas" },
                    { 251, 15.88m, 9.07m, "Švedski valhund" },
                    { 252, 18.14m, 11.79m, "Tajvanski pas" },
                    { 253, 11.34m, 3.63m, "Teddy Roosevelt terijer" },
                    { 254, 34.02m, 15.88m, "Tajlandski ridžbek" },
                    { 255, 68.04m, 31.75m, "Tibetanski mastif" },
                    { 256, 6.80m, 4.08m, "Tibetanski španijel" },
                    { 257, 13.61m, 8.16m, "Tibetanski terijer" },
                    { 258, 49.90m, 28.12m, "Tornjak" },
                    { 259, 90.72m, 45.36m, "Tosa" },
                    { 260, 3.18m, 1.59m, "Toy foksterijer" },
                    { 261, 34.93m, 24.95m, "Erdeljski gonič" },
                    { 262, 22.68m, 13.61m, "Tenesi prugasti gonič" },
                    { 263, 31.75m, 22.68m, "Treeing voker kunhaund" },
                    { 264, 27.22m, 19.96m, "Mađarski ptičar (vižla)" },
                    { 265, 40.82m, 24.95m, "Vajmarski ptičar" },
                    { 266, 24.95m, 15.88m, "Velški springer španijel" },
                    { 267, 9.53m, 9.07m, "Velški terijer" },
                    { 268, 9.07m, 6.80m, "Vest hajlend vajt terijer" },
                    { 269, 34.02m, 22.68m, "Veterhun" },
                    { 270, 18.14m, 11.34m, "Vipet" },
                    { 271, 8.16m, 6.80m, "Oštrodlaki foksterijer" },
                    { 272, 31.75m, 15.88m, "Oštrodlaki pointing grifon" },
                    { 273, 29.48m, 20.41m, "Oštrodlaki mađarski ptičar" },
                    { 274, 27.22m, 12.70m, "Radni kelpi" },
                    { 275, 24.95m, 4.54m, "Ksoloitzkuintle" },
                    { 276, 24.95m, 18.14m, "Jakutska lajka" },
                    { 277, 3.18m, 1.81m, "Jorkširski terijer" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 132);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 133);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 134);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 142);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 143);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 144);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 145);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 146);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 147);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 148);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 149);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 150);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 151);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 152);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 153);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 154);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 155);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 156);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 157);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 158);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 159);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 160);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 161);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 162);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 163);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 164);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 165);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 166);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 167);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 168);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 169);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 170);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 171);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 172);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 173);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 174);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 175);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 176);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 177);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 178);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 179);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 180);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 181);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 182);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 183);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 184);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 185);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 186);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 187);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 188);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 189);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 190);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 191);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 192);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 193);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 194);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 195);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 196);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 197);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 198);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 199);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 202);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 203);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 204);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 208);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 209);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 210);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 211);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 212);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 213);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 214);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 215);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 216);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 217);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 218);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 219);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 220);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 221);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 222);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 223);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 224);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 225);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 226);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 227);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 228);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 229);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 230);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 231);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 232);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 233);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 234);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 235);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 236);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 237);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 238);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 239);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 240);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 241);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 242);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 243);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 244);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 245);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 246);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 247);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 248);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 249);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 250);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 251);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 252);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 253);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 254);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 255);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 256);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 257);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 258);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 259);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 260);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 261);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 262);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 263);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 264);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 265);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 266);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 267);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 268);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 269);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 270);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 271);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 272);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 273);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 274);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 275);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 276);

            migrationBuilder.DeleteData(
                table: "Breeds",
                keyColumn: "Id",
                keyValue: 277);
        }
    }
}
