using DeveLanCacheUI_Backend.Db;

namespace DeveLanCacheUI_Backend.Steam
{
    public static class SimpleSteamDataSeeder
    {
        public static async Task GoSeed(IServiceProvider services)
        {
            await using (var scope = services.CreateAsyncScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                //Steamworks Common Redistributables
                //await dbContext.SeedDataAsync(228980, 228981, 228982, 228983, 228984, 228985, 228986, 228987, 228988, 228989, 228990, 229000, 229001, 229002, 229003, 229004, 229005, 229006, 229007, 229010, 229011, 229012, 229020, 229030, 229031, 229032, 229033);


                //await dbContext.SeedDataAsync(799600, 799601, 799602, 799603); //Cosmoteer
                //await dbContext.SeedDataAsync(453090, 453091, 453092, 453093, 453094); //Parkitect
                //await dbContext.SeedDataAsync(434170, 434171, 434172, 434173, 434174, 434175); //Jack box party pack 3
                //await dbContext.SeedDataAsync(945360, 945361, 945362); //Among Us
                //await dbContext.SeedDataAsync(95400, 95400, 95401, 95402, 95403); //Ibb obb
                //await dbContext.SeedDataAsync(552500, 552501, 552502, 552503, 552504, 737040, 878550); //Warhammer Vermintide 2





                //await dbContext.SeedDataAsync(); //
            }
        }
    }
}
