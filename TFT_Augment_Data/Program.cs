using System.Text.Json.Nodes;
using TFT_Augment_Data_Classes;

//--------------------------------------------------------------------------------------
// Initialisation
Console.WriteLine("Initialisation ...");

const string fileNameLeaderboard = "leaderboard.json";
const string fileNamePlayerData = "player_json/";
const string fileNameCSV = "augment_mean.csv";
string URL_leaderboard = "https://tactics.tools/leaderboards";
string URL_base_profile = "https://tactics.tools/player";
//string URL_json = "https://tactics.tools/_next/data/7bhkIn9qFFt2sSlzc_PqD/en/leaderboards.json";

bool flag_debug = false;
string region = "All";
int nb_top_player = 500; //max 500
int nb_match_player = 50; //max 50

//--------------------------------------------------------------------------------------
// Functions

string convertRegion(string region)
{
    if (region == "br1") { return "br"; }
    if (region == "eun1") { return "eune"; }
    if (region == "euw1") { return "euw"; }
    if (region == "jp1") { return "jp"; }
    if (region == "kr") { return "kr"; } //o
    if (region == "la1") { return "lan"; }
    if (region == "la2") { return "las"; }
    if (region == "na1") { return "na"; }
    if (region == "oc1") { return "oce"; }
    if (region == "tr1") { return "tr"; }
    if (region == "ru") { return "ru"; } //o
    if (region == "ph2") { return "ph"; }
    if (region == "sg2") { return "sg"; }
    if (region == "th2") { return "th"; }
    if (region == "tw2") { return "tw"; }
    if (region == "vn2") { return "vn"; }
    return region;
}

string convertAugment(string augment)
{
    string[] t = augment.Split("Augment_", System.StringSplitOptions.RemoveEmptyEntries);
    if (t[1] != null) { return t[1]; }
    return augment;
}

async Task<string> getJsonFromUrl(string URL)
{
    var client = new HttpClient();
    string delimiter1 = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    string delimiter2 = "</script></body></html>";
    string responseBody = "";

    try
    {
        using HttpResponseMessage response = await client.GetAsync(URL);
        response.EnsureSuccessStatusCode();
        responseBody = await response.Content.ReadAsStringAsync();

        //Console.WriteLine(responseBody);
        string[] TempStr = responseBody.Split(delimiter1, System.StringSplitOptions.RemoveEmptyEntries);
        if (TempStr.Length >= 2)
        {
            responseBody = TempStr[1];
        }
        TempStr = responseBody.Split(delimiter2, System.StringSplitOptions.RemoveEmptyEntries);
        if (TempStr.Length >= 1)
        {
            responseBody = TempStr[0];
        }
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine("\nCannot read url: " + URL);
        Console.WriteLine("Message :{0} ", e.Message);
    }

    return responseBody;
}

async Task<(string[] names,string[] regions)> extractLeaderboardPlayer(int numberOfPlayer, string URL)
{
    string[] playerNames = new string[numberOfPlayer];
    string[] playerRegions = new string[numberOfPlayer];


    string responseBody = await getJsonFromUrl(URL);

    File.WriteAllText(fileNameLeaderboard, responseBody);

    // Extract all names from json leaderboard

    var jsonLeaderboardObject = JsonNode.Parse(responseBody).AsObject();
    var listOfPlayerData = jsonLeaderboardObject["props"]["pageProps"]["data"][1].AsArray();
    for (int i = 0; i < listOfPlayerData.Count; i++)
    {
        if (i >= numberOfPlayer) { break; }
        var jsonPlayerDataObject = listOfPlayerData[i].AsObject();
        playerNames[i] = jsonPlayerDataObject["playerName"].ToString();
        playerRegions[i] = jsonPlayerDataObject["region"].ToString();
    }

    if (flag_debug)
    {
        for (int z = 0; z < playerNames.Length; z++)
        {
            Console.WriteLine("Name: " + playerNames[z] + " / Region: " + playerRegions[z]);
        }
    }



    return (playerNames, playerRegions);
}

async Task<List<Match>> extractMatchOfAPlayer(string playerName, string playerRegion)
{
    string profile_URL = URL_base_profile + "/" + convertRegion(playerRegion) + "/" + playerName;
    if (flag_debug) { Console.WriteLine(profile_URL); }
    string profileJson = await getJsonFromUrl(profile_URL);
    File.WriteAllText(fileNamePlayerData + playerName + "_profile.json", profileJson);

    var jsonProfileObject = JsonNode.Parse(profileJson).AsObject();
    List<Match> ArrayOfMatchs = new List<Match>();
    try
    {
        var listOfMatches = jsonProfileObject["props"]["pageProps"]["initialData"]["matches"].AsArray();

        if (flag_debug) { Console.WriteLine("Nombre de match: " + listOfMatches.Count); }
        for (int j = 0; j < listOfMatches.Count; j++)
        {
            if (j >= nb_match_player) { break; }
            if (flag_debug) { Console.WriteLine("Match nb°:" + j); }
            try
            {
                var matchData = listOfMatches[j].AsObject();
                string match_id = matchData["id"].ToString();
                string match_patch = matchData["patch"].ToString();
                string match_placement = matchData["info"]["placement"].ToString();
                var listOfAugments = matchData["info"]["augments"].AsArray();
                string[] listOfAugmentName = new string[3];
                if (listOfAugments.Count == 3)
                {
                    listOfAugmentName[0] = listOfAugments[0].ToString();
                    listOfAugmentName[1] = listOfAugments[1].ToString();
                    listOfAugmentName[2] = listOfAugments[2].ToString();
                }
                else if (listOfAugments.Count == 2)
                {
                    listOfAugmentName[0] = listOfAugments[0].ToString();
                    listOfAugmentName[1] = listOfAugments[1].ToString();
                }
                else if (listOfAugments.Count == 1)
                {
                    listOfAugmentName[0] = listOfAugments[0].ToString();
                }
                Match match_unity = new Match(match_id, convertRegion(playerRegion), match_patch, listOfAugmentName, match_placement);
                ArrayOfMatchs.Add(match_unity);
            }
            catch (Exception e)
            {
                if (flag_debug) { Console.WriteLine(e.Message); Console.WriteLine("Match discard invalid data"); }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message); Console.WriteLine("Failed to load games");
    }
    return ArrayOfMatchs;

}

List<string> getListOfAugmentUnique(List<Match> MegaListOfMatchOfAllPlayers)
{
    List<string> augList = new List<string>();
    for (int i = 0; i < MegaListOfMatchOfAllPlayers.Count; i++)
    {
        for (int j = 0; j < MegaListOfMatchOfAllPlayers[i].augments.Length; j++)
        {
            string nameAugment = MegaListOfMatchOfAllPlayers[i].augments[j];
            if (nameAugment != null && !augList.Contains(nameAugment))
            {
                augList.Add(nameAugment);
            }
        }
    }
    if (flag_debug) {foreach (string aug in augList) {Console.WriteLine(aug);}}
    return augList;
}

void calculMeanCSV(List<string> augList, List<Match> MegaListOfMatchOfAllPlayers)
{
    string csv_file = "Augment name;Average placement\r\n";
    foreach (string aug in augList)
    {
        float nb_appararition = 0;
        float total_placement = 0;
        for (int i = 0; i < MegaListOfMatchOfAllPlayers.Count; i++)
        {
            if (MegaListOfMatchOfAllPlayers[i].augments.Contains(aug))
            {
                nb_appararition++;
                total_placement += float.Parse(MegaListOfMatchOfAllPlayers[i].placement);
            }
        }
        if (flag_debug)
        {
            Console.WriteLine("Placement moyen de " + convertAugment(aug) + ": " + total_placement / nb_appararition);
        }
        csv_file += convertAugment(aug) + ";" + total_placement / nb_appararition + "\r\n";
    }
    File.WriteAllText(fileNameCSV, csv_file);
}

//---------------------------------------------------------------------------------------
// Main
async Task<int> Main()
{
    // Loading config Parameter
    Console.WriteLine("Loading configurations parameters ...");
    Console.WriteLine("Select a region (All/BR/EUNE/EUW/JP/KR/LAN/LAS/NA/OCE/TR/RU/PH/SG/TH/TW/VN) (All by default):");
    string input_string = Console.ReadLine();
    if (input_string != null && input_string != "")
    {
        region = input_string;
        if (region != "All") { URL_leaderboard = URL_leaderboard + "/" + region; }
    }
    Console.WriteLine("Select the number of top player in leaderboard (500 max and by default):");
    input_string = Console.ReadLine();
    if (input_string != null && input_string != "")
    {
        nb_top_player = int.Parse(input_string);
        if (nb_top_player > 500) { nb_top_player = 500; }
    }
    Console.WriteLine("Select the number of games per player (50 max and by default):");
    input_string = Console.ReadLine();
    if(input_string != null && input_string != "")
    {
        nb_match_player = int.Parse(input_string);
        if (nb_match_player > 50) { nb_match_player = 50; }
    }

    // Extract Leaderboard
    Console.WriteLine("Leaderboard extraction ...");
    var tupleNameRegion = await extractLeaderboardPlayer(nb_top_player, URL_leaderboard);
    string[] playerNames = new string[nb_top_player];
    string[] playerRegions = new string[nb_top_player];
    playerNames = tupleNameRegion.names;
    playerRegions = tupleNameRegion.regions;

    // Get Player Profile Matchs
    Console.WriteLine("Matchs extraction ... (may take a while)");
    List<Match> MegaListOfMatchOfAllPlayers = new List<Match>();
    for (int i = 0; i < playerNames.Length && i < playerRegions.Length; i++)
    {
        Console.WriteLine("Extracting " + playerNames[i] + "'s games ...");
        List<Match> t = await extractMatchOfAPlayer(playerNames[i], playerRegions[i]);
        for (int j = 0; j < t.Count; j++)
        {
            MegaListOfMatchOfAllPlayers.Add(t[j]);
        }
    }
    if (flag_debug) {Console.WriteLine("Nombre de matchs totaux enregistrés: " + MegaListOfMatchOfAllPlayers.Count);}

    // Calcul Time
    Console.WriteLine("Mean calculation and CSV file generation ...");
    List<string> augList = getListOfAugmentUnique(MegaListOfMatchOfAllPlayers);
    calculMeanCSV(augList, MegaListOfMatchOfAllPlayers);

    return 1;
}

await Main();