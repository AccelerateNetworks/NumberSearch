namespace NumberSearch.DataAccess.Models
{
    public static class AreaCode
    {
        public static readonly int[] All = new int[]
        {
            201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,223,224,225,226,227,228,229,231,234,235,239,240,242,246,248,249,250,251,252,253,254,256,260,262,263,264,267,268,269,270,272,274,276,278,279,281,283,284,289,
            301,302,303,304,305,306,307,308,309,310,311,312,313,314,315,316,317,318,319,320,321,323,325,326,327,330,331,332,334,336,337,339,340,341,343,345,346,347,351,352,360,361,364,365,367,368,380,381,385,386,387,
            401,402,403,404,405,406,407,408,409,410,411,412,413,414,415,416,417,418,419,423,424,425,428,430,431,432,434,435,437,438,440,441,442,443,445,447,450,456,458,463,464,468,469,470,473,474,475,478,479,480,484,
            500,501,502,503,504,505,506,507,508,509,510,511,512,513,514,515,516,517,518,519,520,521,522,530,531,532,533,534, 535,538,539,540,541,548,551,559,561,562,563,564,567,570,571,573,574,577,578,579,580,581,584,585,586,
            600,601,602,603,604,605,606,607,608,609,610,612,613,614,615,616,617,618,619,620,622,623,626,628,629,630,631,636,639,640,641,646,647,649,650,651,657,658,659,660,661,662,664,667,669,670,671,672,678,679,680,681,682,683,684,689,
            700,701,702,703,704,705,706,707,708,709,710,711,712,713,714,715,716,717,718,719,720,721,724,725,726,727,730,731,732,734,737,740,742,743,747,753,754,757,758,760,762,763,765,767,769,770,772,773,774,775,778,779,780,781,782,784,785,786,787,
            800,801,802,803,804,805,806,807,808,809,810,811,812,813,814,815,816,817,818,819,820,825,828,829,830,831,832,833,838,839,840,843,844,845,847,848,849,850,854,855,856,857,858,859,860,862,863,864,865,866,867,868,869,870,872,873,876,877,878,879,888,889,
            900,901,902,903,904,905,906,907,908,909,910,912,913,914,915,916,917,918,919,920,925,927,928,929,930,931,934,935,936,937,938,939,940,941,947,949,951,952,954,956,959,970,971,972,973,978,979,980,984,985,986,989
        };

        public static readonly int[] Priority = new int[]
        {
            206,360,425,503,509,541,564
        };

        public static readonly int[] TollFree = new int[]
        {
            800, 833, 844, 855, 866, 877, 888
        };

        public class AreaCodesByState
        {
            public string State { get; set; }
            public string StateShort { get; set; }
            public int[] AreaCodes { get; set; }
        }

        public static readonly AreaCodesByState[] States = new AreaCodesByState[]
        {
            new AreaCodesByState
            {
                State = "Alabama",
                StateShort = "AL",
                AreaCodes = new int[]
                {
                    205, 251, 256, 334, 938
                }
            },
            new AreaCodesByState
            {
                State = "Alaska",
                StateShort = "AK",
                AreaCodes = new int[]
                {
                    907
                }
            },
            new AreaCodesByState
            {
                State = "Arizona",
                StateShort = "AZ",
                AreaCodes = new int[]
                {
                    480, 520, 602, 623, 928
                }
            },
            new AreaCodesByState
            {
                State = "Arkansas",
                StateShort = "AR",
                AreaCodes = new int[]
                {
                    479, 501, 870
                }
            },
            new AreaCodesByState
            {
                State = "California",
                StateShort = "CA",
                AreaCodes = new int[]
                {
                    209, 213, 279, 310, 323, 408, 415, 424, 442, 510, 530, 559, 562, 619, 626, 628, 650, 657, 661, 669, 707, 714, 747, 760, 805, 818, 820, 831, 858, 909, 916, 925, 949, 951
                }
            },
            new AreaCodesByState
            {
                State = "Colorado",
                StateShort = "CO",
                AreaCodes = new int[]
                {
                    303, 719, 720, 970
                }
            },
            new AreaCodesByState
            {
                State = "Connecticut",
                StateShort = "CT",
                AreaCodes = new int[]
                {
                    203, 475, 860, 959
                }
            },
            new AreaCodesByState
            {
                State = "Delaware",
                StateShort = "DE",
                AreaCodes = new int[]
                {
                    302
                }
            },
            new AreaCodesByState
            {
                State = "Florida",
                StateShort = "FL",
                AreaCodes = new int[]
                {
                    239, 305, 321, 352, 386, 407, 561, 727, 754, 772, 786, 813, 850, 863, 904, 941, 954
                }
            },
            new AreaCodesByState
            {
                State = "Georgia",
                StateShort = "GA",
                AreaCodes = new int[]
                {
                    229, 404, 470, 478, 678, 706, 762, 770, 912
                }
            },
            new AreaCodesByState
            {
                State = "Hawaii",
                StateShort = "HI",
                AreaCodes = new int[]
                {
                   808
                }
            },
            new AreaCodesByState
            {
                State = "Idaho",
                StateShort = "ID",
                AreaCodes = new int[]
                {
                   208, 986
                }
            },
            new AreaCodesByState
            {
                State = "Illinois",
                StateShort = "IL",
                AreaCodes = new int[]
                {
                   217, 224, 309, 312, 331, 618, 630, 708, 773, 779, 815, 847, 872
                }
            },
            new AreaCodesByState
            {
                State = "Indiana",
                StateShort = "IN",
                AreaCodes = new int[]
                {
                   219, 260, 317, 463, 574, 765, 812, 930
                }
            },
            new AreaCodesByState
            {
                State = "Iowa",
                StateShort = "IA",
                AreaCodes = new int[]
                {
                   319, 515, 563, 641, 712
                }
            },
            new AreaCodesByState
            {
                State = "Kansas",
                StateShort = "KS",
                AreaCodes = new int[]
                {
                    316, 620, 785, 913
                }
            },
            new AreaCodesByState
            {
                State = "Kentucky",
                StateShort = "KY",
                AreaCodes = new int[]
                {
                    270, 364, 502, 606, 859
                }
            },
            new AreaCodesByState
            {
                State = "Louisiana",
                StateShort = "LA",
                AreaCodes = new int[]
                {
                    225, 318, 337, 504, 985
                }
            },
            new AreaCodesByState
            {
                State = "Maine",
                StateShort = "ME",
                AreaCodes = new int[]
                {
                    207
                }
            },
            new AreaCodesByState
            {
                State = "Maryland",
                StateShort = "MD",
                AreaCodes = new int[]
                {
                    240, 301, 410, 443, 667
                }
            },
            new AreaCodesByState
            {
                State = "Massachusetts",
                StateShort = "MA",
                AreaCodes = new int[]
                {
                    339, 351, 413, 508, 617, 774, 781, 857, 978
                }
            },
            new AreaCodesByState
            {
                State = "Michigan",
                StateShort = "MI",
                AreaCodes = new int[]
                {
                    231, 248, 269, 313, 517, 586, 616, 734, 810, 906, 947, 989
                }
            },
            new AreaCodesByState
            {
                State = "Minnesota",
                StateShort = "MN",
                AreaCodes = new int[]
                {
                    218, 320, 507, 612, 651, 763, 952
                }
            },
            new AreaCodesByState
            {
                State = "Mississippi",
                StateShort = "MS",
                AreaCodes = new int[]
                {
                    228, 601, 662, 769
                }
            },
            new AreaCodesByState
            {
                State = "Missouri",
                StateShort = "MO",
                AreaCodes = new int[]
                {
                    314, 417, 573, 636, 660, 816
                }
            },
            new AreaCodesByState
            {
                State = "Montana",
                StateShort = "MT",
                AreaCodes = new int[]
                {
                    406
                }
            },
            new AreaCodesByState
            {
                State = "Nebraska",
                StateShort = "NE",
                AreaCodes = new int[]
                {
                    308, 402, 531
                }
            },
            new AreaCodesByState
            {
                State = "Nevada",
                StateShort = "NV",
                AreaCodes = new int[]
                {
                    702, 725, 775
                }
            },
            new AreaCodesByState
            {
                State = "New Hampshire",
                StateShort = "NH",
                AreaCodes = new int[]
                {
                    603
                }
            },
            new AreaCodesByState
            {
                State = "New Jersey",
                StateShort = "NJ",
                AreaCodes = new int[]
                {
                    201, 551, 609, 640, 732, 848, 856, 862, 908, 973
                }
            },
            new AreaCodesByState
            {
                State = "New Mexico",
                StateShort = "NM",
                AreaCodes = new int[]
                {
                    505, 575
                }
            },
            new AreaCodesByState
            {
                State = "New York",
                StateShort = "NY",
                AreaCodes = new int[]
                {
                    212, 315, 332, 347, 516, 518, 585, 607, 631, 646, 680, 716, 718, 838, 845, 914, 917, 929, 934
                }
            },
            new AreaCodesByState
            {
                State = "North Carolina",
                StateShort = "NC",
                AreaCodes = new int[]
                {
                    252, 336, 704, 743, 828, 910, 919, 980, 984
                }
            },
            new AreaCodesByState
            {
                State = "Ohio",
                StateShort = "OH",
                AreaCodes = new int[]
                {
                    216, 220, 234, 330, 380, 419, 440, 513, 567, 614, 740, 937
                }
            },
            new AreaCodesByState
            {
                State = "Oklahoma",
                StateShort = "OK",
                AreaCodes = new int[]
                {
                    405, 539, 580, 918
                }
            },
            new AreaCodesByState
            {
                State = "Oregon",
                StateShort = "OR",
                AreaCodes = new int[]
                {
                    458, 503, 541, 971
                }
            },
            new AreaCodesByState
            {
                State = "Pennsylvania",
                StateShort = "PA",
                AreaCodes = new int[]
                {
                    215, 223, 267, 272, 412, 445, 484, 570, 610, 717, 724, 814, 878
                }
            },
            new AreaCodesByState
            {
                State = "Rhode Island",
                StateShort = "RI",
                AreaCodes = new int[]
                {
                    401
                }
            },
            new AreaCodesByState
            {
                State = "South Carolina",
                StateShort = "SC",
                AreaCodes = new int[]
                {
                    803, 843, 854, 864
                }
            },
            new AreaCodesByState
            {
                State = "South Dakota",
                StateShort = "SD",
                AreaCodes = new int[]
                {
                    605
                }
            },
            new AreaCodesByState
            {
                State = "Tennessee",
                StateShort = "TN",
                AreaCodes = new int[]
                {
                    423, 615, 629, 731, 865, 901, 931
                }
            },
            new AreaCodesByState
            {
                State = "Texas",
                StateShort = "TX",
                AreaCodes = new int[]
                {
                    210, 214, 254, 281, 325, 346, 361, 409, 430, 432, 469, 512, 682, 713, 726, 737, 806, 817, 830, 832, 903, 915, 936, 940, 956, 972, 979
                }
            },
            new AreaCodesByState
            {
                State = "Utah",
                StateShort = "UT",
                AreaCodes = new int[]
                {
                    385, 435, 801
                }
            },
            new AreaCodesByState
            {
                State = "Vermont",
                StateShort = "VT",
                AreaCodes = new int[]
                {
                    802
                }
            },
            new AreaCodesByState
            {
                State = "Virginia",
                StateShort = "VA",
                AreaCodes = new int[]
                {
                    276, 434, 540, 571, 703, 757, 804
                }
            },
            new AreaCodesByState
            {
                State = "Washington",
                StateShort = "WA",
                AreaCodes = new int[]
                {
                    206, 253, 360, 425, 509, 564
                }
            },
            new AreaCodesByState
            {
                State = "Washington, DC",
                StateShort = "DC",
                AreaCodes = new int[]
                {
                    202
                }
            },
            new AreaCodesByState
            {
                State = "West Virginia",
                StateShort = "WV",
                AreaCodes = new int[]
                {
                    304, 681
                }
            },
            new AreaCodesByState
            {
                State = "Wisconsin",
                StateShort = "WI",
                AreaCodes = new int[]
                {
                    262, 414, 534, 608, 715, 920
                }
            },
            new AreaCodesByState
            {
                State = "Wyoming",
                StateShort = "WY",
                AreaCodes = new int[]
                {
                    307
                }
            }
        };
    }
}
