using System;
using System.Text;
using Xunit;
using WinCarePro.Infrastructure.Security;

namespace WinCarePro.Tests;

public class CryptoHelperTests
{
    [Fact]
    public void ProtectAndUnprotectString_RoundTrip_PreservesOriginalText()
    {
        string original = "WinCarePro_Secret_Key_123!@#";
        string encrypted = CryptoHelper.ProtectString(original);
        
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(original, encrypted);

        string decrypted = CryptoHelper.UnprotectString(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void ProtectString_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CryptoHelper.ProtectString(""));
        Assert.Equal(string.Empty, CryptoHelper.ProtectString(null!));
    }

    [Fact]
    public void UnprotectString_InvalidBase64_ReturnsInputGracefully()
    {
        string invalid = "Not_A_Valid_Base64_Encrypted_String";
        string result = CryptoHelper.UnprotectString(invalid);
        Assert.Equal(invalid, result);
    }

    [Fact]
    public void ComputeSha256_ComputesCorrectHash()
    {
        byte[] data = Encoding.UTF8.GetBytes("hello");
        string hash = CryptoHelper.ComputeSha256(data);
        // SHA-256 for 'hello' is 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824
        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", hash);
    }
}
