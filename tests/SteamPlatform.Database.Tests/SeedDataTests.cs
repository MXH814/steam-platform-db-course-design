using System.Text.RegularExpressions;

namespace SteamPlatform.Database.Tests;

public sealed class SeedDataTests
{
    [Theory]
    [InlineData("alice")]
    [InlineData("bob")]
    [InlineData("rootadmin")]
    public void Seed_login_accounts_use_non_plaintext_hashes(string account)
    {
        var accountInsert = Regex.Match(
            SqlFile.Data,
            $@"VALUES\s*\([^;]*'{Regex.Escape(account)}'[^;]*'(?<hash>(PBKDF2\$SHA256\$|\$2[aby]\$)[^']+)'",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        Assert.True(accountInsert.Success, $"{account} should have a recognized non-plaintext seed hash.");
    }

    [Fact]
    public void Seed_data_does_not_store_demo_plaintext_passwords()
    {
        Assert.DoesNotContain("'alice', 'alice'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("'bob', 'bob'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("'rootadmin', 'admin'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
    }
}
