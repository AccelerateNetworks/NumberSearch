using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Peerless;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Peerless
    {
        /// <summary>
        /// Get a list of phone number from a list of area codes.
        /// </summary>
        /// <param name="areaCodes"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(int[] areaCodes, string apiKey)
        {
            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await DidFind.GetByNPAAsync(code.ToString(), apiKey).ConfigureAwait(false));
                    Log.Information($"[Peerless] Found {numbers.Count} Phone Numbers for NPA {code}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[Peerless] Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return numbers.ToArray();
        }


        /// <summary>
        /// Get a list of valid phone numbers from a list of rate centers.
        /// </summary>
        /// <param name="ratecenters"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByRateCenterAsync(string[] ratecenters, string apiKey)
        {
            var numbers = new List<PhoneNumber>();

            foreach (var ratecenter in ratecenters)
            {
                try
                {
                    numbers.AddRange(await DidFind.GetByRateCenterAsync(ratecenter, apiKey).ConfigureAwait(false));
                    Log.Information($"[Peerless] Found {numbers.Count} Phone Numbers for Rate Center {ratecenter}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[Peerless] Rate Center {ratecenter} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return numbers.ToArray();
        }

        /// <summary>
        /// Valid Peerless rate centers as they cannot be query via API call.
        /// </summary>
        public static readonly string[] RateCenters206 = { "BAINBDG IS", "DES MOINES", "RICHMNDBCH", "SEATTLE ", "VASHON " };
        public static readonly string[] RateCenters253 = { "AUBURN ", "DES MOINES", "GIG HARBOR", "GRAHAM ", "KENT ", "PUYALLUP ", "ROY ", "SUMNER ", "TACOMA ", "TACOMAWVLY" };
        public static readonly string[] RateCenters360 = { "ABERDEEN ", "AMBOY ", "ASHFORD ", "BATTLEGRND", "BELFAIR ", "BLACKDIMND", "BREMERTON ", "BUCKLEY ", "BUCODA ", "CAMAS ", "CASTLEROCK", "CATHLAMET ", "CENTRALIA ", "CHEHALIS ", "CHIMACMCTR", "COPALIS ", "COUGAR ", "CRYSTAL MT", "CURTIS ", "DEWATTO ", "EATONVILLE", "ELMA ", "ENUMCLAW ", "FORKS ", "GRAYHARBCO", "GRAYSRIVER", "HAT ISLAND", "HOOD CANAL", "HOODSPORT ", "KALAMA ", "KINGSTON ", "LA CENTER ", "LKQUINAULT", "LONG BEACH", "LONGVIEW ", "MARYSVILLE", "MONTESANO ", "MORTON ", "MOSSYROCK ", "MT VERNON ", "NASELLE ", "OAK HARBOR", "OCOSTA ", "OLYMPIA ", "ORTING ", "PACIFICBCH", "PORT ORCH ", "PORTLUDLOW", "POULSBO ", "PT ANGELES", "PT ROBERTS", "PTTOWNSEND", "RIDGEFIELD", "ROCHESTER ", "SALKUM ", "SAN JUAN ", "SHELTON ", "SILVERDALE", "SKYKOMISH ", "SNOHOMISH ", "SO PRAIRIE", "SO WHIDBEY", "SOUTH BEND", "STEVESPASS", "TENINO ", "TOLEDO ", "UNION ", "VADER ", "VANCOUVER ", "WHATCOMCTY", "WINLOCK ", "WOODLAND ", "YACOLT ", "YALE ", "YELM " };
        public static readonly string[] RateCenters425 = { "AMES LAKE ", "BELLEVUE ", "BOTHELL ", "CARNATION ", "EVERETT ", "FALL CITY ", "HALLS LAKE", "ISSAQUAH ", "KIRKLAND ", "MAPLE VLY ", "NORTH BEND", "RENTON ", "SILVERLAKE", "SNOQUMPASS" };
        public static readonly string[] RateCenters503 = { "AMITY ", "ASTORIA ", "AUMSVLTRNR", "AURORA ", "BAY CITY ", "BEAVER ", "BEAVER CRK", "BEAVERTON ", "CANBYNEEDY", "CANNON BCH", "CARLTON ", "CHARBONNEU", "CLACKAMAS ", "CLATSKANIE", "CLOVERDALE", "COLTON ", "CORBETT ", "DALLAS ", "DAYTON ", "DETROIT ", "ESTACADA ", "FALLS CITY", "GARIBALDI ", "GERVAIS ", "GOVENTCAMP", "GRAND IS ", "GRANDRONDE", "GRESHAM ", "HOOD LAND ", "JEWELL ", "KNAPPA ", "LYONS ", "MCMINNVL ", "MILL CITY ", "MOLALLA ", "MONITOR ", "MOUNTANGEL", "NEHALEM ", "NEWBERG ", "NORTH PL ", "PACIFIC CY", "PORTLAND ", "RAINIER ", "REDLAND ", "ROCKAWAY ", "SALEM ", "SCAPPOOSE ", "SCIO ", "SEASIDE ", "SHERIDAN ", "SHERWOOD ", "SILVERTON ", "ST HELENS ", "ST PAUL ", "STAYTON ", "STFRD-SNDY", "TILLAMOOK ", "VERNONIA ", "WARRENTON ", "WESTPORT ", "WILLAMINA ", "WOODBURN ", "YAMHILL " };
        public static readonly string[] RateCenters509 = { "ALMIRA ", "ANATONE ", "ASOTIN ", "BENTONCITY", "BREWSTER ", "BRIDGEPORT", "CHENEY ", "CHEWELAH ", "CLARKSTON ", "CLE ELUM ", "COLFAX ", "COLUMBIA ", "COLVILLE ", "CONNELL ", "COULEE DAM", "COULEECITY", "COWICHE ", "CRESTON ", "CURLEW ", "CUSICK ", "DALLESPORT", "DAVENPORT ", "DAYTON ", "DEER PARK ", "DOUGLASCO ", "EDWALLTYLR", "ELK-GRNBLF", "ELLENSBURG", "ENDICOTT ", "EPHRATA ", "EUREKA ", "FARMINGTON", "GARFIELD ", "GARRISON ", "GEORGE ", "GLENWOOD ", "GOLDENDALE", "GRANDVIEW ", "GRANGER ", "HARRAH ", "HARRINGTON", "IONE ", "KENNEWICK ", "KETTLE FLS", "KLICKITAT ", "LACROSSE ", "LIBERTY LK", "LIND ", "LOOMIS ", "LOON LAKE ", "LYLE ", "MABTON ", "MATHEWSCOR", "MATTAWA ", "MEDICAL LK", "METALINFLS", "MOLSON ", "MOSES LAKE", "MT HULL ", "NACHES ", "NESPELEM ", "NEWMANLAKE", "NEWPORT ", "NILE ", "NORTHPORT ", "OAKESDALE ", "ODESSA ", "OMAK ", "OROVILLE ", "OTHELLO ", "PALOUSE ", "PASCO ", "PATEROS ", "PATERSON ", "POMEROY ", "PRESCOTT ", "PROSSER ", "PULLMAN ", "QUINCY ", "REARDAN ", "REPUBLIC ", "RICHLAND ", "RIMROCK ", "RITZVILLE ", "ROCKFORD ", "ROOSEVELT ", "ROSALIA ", "ROSLYN ", "SELAH ", "SOAP LAKE ", "SPANGLE ", "SPOKANE ", "SPRAGUE ", "SPRINGDALE", "ST JOHN ", "STARBUCK ", "STEVENSON ", "SUNNYSIDE ", "TEKOA ", "TIETON ", "TONASKET ", "TOPPENISH ", "TOUCHET ", "TROUT LAKE", "TWISP ", "UNIONTOWN ", "WAITSBURG ", "WALLAWALLA", "WAPATO ", "WARDEN ", "WASHTUCNA ", "WENATCHEE ", "WH SALMON ", "WHITE SWAN", "WHITSTRAN ", "WILBUR ", "WILLARD ", "WILSON CRK", "YAKIMA " };
        public static readonly string[] RateCenters541 = { "ADRIAN ", "ALBANY ", "ALSEA ", "ANTELOPE ", "ARLINGTON ", "ASH VALLEY", "ASHLAND ", "ATHENA ", "AZALEA ", "BAKER ", "BANDON ", "BATES ", "BELLFONTAN", "BEND ", "BLACKBUTTE", "BLODGETT ", "BLUE RIVER", "BLY ", "BOARDMAN ", "BONANZA ", "BROOKINGS ", "BROWNSVL ", "BURNS ", "BUTTEFALLS", "CAMAS VLY ", "CANYONVL ", "CASCADELKS", "CAVE JCT ", "CENTRAL PT", "CHEMULT ", "CHILOQUIN ", "CHITWOOD ", "CONDON ", "COOS BAY ", "COQUILLE ", "CORVALLIS ", "COTTAGEGRV", "COVE ", "CRATERLAKE", "CRESWELL ", "CULVER ", "DAYS CREEK", "DAYVILLE ", "DEPOE BAY ", "DIAMOND LK", "DRAIN ", "DUFUR ", "DURKEE ", "ECHO ", "ELGIN ", "ELKTON ", "ENTERPRISE", "EUGENE ", "FISH LAKE ", "FLORA TROY", "FLORENCE ", "FOSSIL ", "FT KLAMATH", "GILCHRIST ", "GLENDALE ", "GLIDE ", "GOLD BEACH", "GOLD HILL ", "GRANTSPASS", "GRASS VLY ", "HAINES ", "HALFWAY ", "HALSEY ", "HARLAN ", "HARPER ", "HARRISBURG", "HELIX ", "HEPPNER ", "HEREFDUNTY", "HERMISTON ", "HOOD RIVER", "HORTON ", "HUNTINGTON", "IMBLER ", "IONE ", "JACKSONVL ", "JEFFERSON ", "JOHN DAY ", "JORDAN VLY", "JOSEPH ", "JUNCTIONCY", "JUNTURA ", "KLAMATHFLS", "LA GRANDE ", "LAKESIDE ", "LAKEVIEW ", "LANGLOIS ", "LAPINE ", "LEABURG ", "LEBANON ", "LEXINGTON ", "LINCOLN CY", "LOBSTERVLY", "LONG CREEK", "LOSTINE ", "LOWELL ", "MADRAS ", "MALIN ", "MAPLETON ", "MARCOLA ", "MAUPIN ", "MEACHAM ", "MEDFORD ", "MEDICALSPG", "MERRILL ", "MILTONFWTR", "MITCHELL ", "MONROE ", "MONUMENT ", "MORO ", "MOSIER ", "MT VERNON ", "MYRTLE CRK", "MYRTLE PT ", "NEWPORT ", "NO HARNEY ", "NO POWDER ", "NO UMPQUA ", "NYSSA ", "O BRIEN ", "OAKLAND ", "OAKRIDGE ", "ODELL ", "ONTARIO ", "OREGONSLOP", "OXBOW ", "PAISLEY ", "PARKDALE ", "PAULINA ", "PENDLETON ", "PHILOMATH ", "PHOENIX ", "PILOT ROCK", "PINE GROVE", "PORTORFORD", "POWERS ", "PRAIRIE CY", "PRINEVILLE", "PROSPECT ", "PROVOTMPHY", "QUINN ", "REDMOND ", "REEDSPORT ", "RICHLAND ", "RIDDLE ", "RIDGEVIEW ", "ROCKYPOINT", "ROGUERIVER", "ROSEBURG ", "RUFUS ", "SCOTTSBURG", "SELMA ", "SENECA ", "SHADY COVE", "SHEDD ", "SILETZ ", "SILVERLAKE", "SISTERS ", "SO HARNEY ", "SOUTHBEACH", "SPRAGUERIV", "SPRAY ", "STANFIELD ", "STATELINE ", "SUMMIT ", "SUMPTER ", "SWEET HOME", "THE DALLES", "THREE RVS ", "TIDEWATER ", "TOLEDO ", "TRIANGLELK", "TYGHVALLEY", "UKIAH ", "UMATILLA ", "UNION ", "VALE ", "VENETA ", "WALDPORT ", "WALLOWA ", "WAMIC ", "WASCO ", "WHITE CITY", "WOLF CREEK", "YACHATS ", "YONCALLA " };
        public static readonly string[] PriorityRateCenters = RateCenters206.Concat(RateCenters253).Concat(RateCenters360).Concat(RateCenters425).Concat(RateCenters503).Concat(RateCenters509).Concat(RateCenters541).ToArray();
    }
}