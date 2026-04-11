#nullable enable
using NUnit.Framework;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;

[TestFixture]
public class FixedStringTests
{
    private const int Cap32 = 16;

    [Test]
    public void Property_Capacity_IsCorrect()
    {
        FixedString32 str32 = new();
        Assert.AreEqual(Cap32, str32.Capacity);
    }

    [Test]
    public void BinarySize_IsCorrect()
    {
        int expectedSize = Cap32 * sizeof(char) + sizeof(ushort);
        int actualSize = Binary<FixedString32>.Size;
        Assert.AreEqual(expectedSize, actualSize);
    }

    [Test]
    public void ConstructorAndConversions_UnderCapacity_MaintainsExactLength()
    {
        string input = "Hello";
        FixedString32 fstr = new(input);

        string output = fstr; // Implicit operator test

        Assert.AreEqual(input.Length, output.Length);
        Assert.AreEqual("Hello", output);
    }

    [Test]
    public void ConstructorAndConversions_ExactCapacity_FitsPerfectly()
    {
        string input = "1234567890123456";
        FixedString32 fstr = (FixedString32)input; // Explicit operator test

        string output = fstr.ToString();

        Assert.AreEqual(Cap32, output.Length);
        Assert.AreEqual(input, output);
    }

    [Test]
    public void ConstructorAndConversions_OverCapacity_TruncatesInput()
    {
        string input = "This string is way too long for a 32-byte struct";
        FixedString32 fstr = new(input);

        string output = fstr;

        Assert.AreEqual(Cap32, output.Length);
        Assert.AreEqual("This string is w", output);
    }

    [Test]
    public void EdgeCase_ExplicitNullsInsideAndAtTheEnd_AreMaintained()
    {
        string input = "A\0B\0C\0\0";
        FixedString32 fstr = new(input);

        string output = fstr;

        Assert.AreEqual(7, output.Length);
        Assert.AreEqual('A', output[0]);
        Assert.AreEqual('\0', output[1]);
        Assert.AreEqual('B', output[2]);
        Assert.AreEqual('\0', output[3]);
        Assert.AreEqual('C', output[4]);
        Assert.AreEqual('\0', output[5]);
        Assert.AreEqual('\0', output[6]);
    }

    [Test]
    public void EdgeCase_Emoji_FitsInside()
    {
        string input = "Rocket 🚀";
        FixedString32 fstr = new(input);

        string output = fstr;
        Assert.AreEqual("Rocket 🚀", output);
        Assert.AreEqual(9, output.Length);
    }

    [Test]
    public void Utils_CutAndJoin_EmojiSplitAcrossBoundary_ReconstructsPerfectly()
    {
        string input = "123456789012345🚀";

        FixedString32[] parts = FixedStringUtils.Cut(input, s => new FixedString32(s));

        Assert.AreEqual(2, parts.Length);

        string part1 = parts[0];
        Assert.AreEqual(Cap32, part1.Length);
        Assert.AreEqual('\uD83D', part1[15]);

        string part2 = parts[1];
        Assert.AreEqual(1, part2.Length);
        Assert.AreEqual('\uDE80', part2[0]);

        string joined = FixedStringUtils.Join(parts);

        Assert.AreEqual(input, joined);
    }

    [Test]
    public void Serialization_RoundTrip_PreservesDataAndLength()
    {
        FixedString32 original = new("Test Data\0");

        byte[] bytes = Binary<FixedString32>.Serialize(original);
        FixedString32 deserialized = Binary<FixedString32>.Deserialize(bytes);

        Assert.AreEqual(original.ToString(), deserialized.ToString());
        Assert.AreEqual(10, deserialized.ToString().Length);
    }

    [Test]
    public void Utils_Cut_EmptyString_ReturnsSingleEmptyStruct()
    {
        FixedString32[] parts = FixedStringUtils.Cut("", s => new FixedString32(s));

        Assert.AreEqual(1, parts.Length);
        Assert.AreEqual(0, parts[0].ToString().Length);
        Assert.AreEqual("", parts[0].ToString());
    }

    [Test]
    public void Utils_Cut_LargeString_SplitsCorrectly()
    {
        string input = new('A', 40);
        FixedString32[] parts = FixedStringUtils.Cut(input, s => new FixedString32(s));

        Assert.AreEqual(3, parts.Length);
        Assert.AreEqual(16, parts[0].ToString().Length);
        Assert.AreEqual(16, parts[1].ToString().Length);
        Assert.AreEqual(8, parts[2].ToString().Length);

        Assert.AreEqual(new string('A', 16), parts[0].ToString());
        Assert.AreEqual(new string('A', 16), parts[1].ToString());
        Assert.AreEqual(new string('A', 8), parts[2].ToString());
    }

    [Test]
    public void Utils_Join_ReconstructsStringExactly()
    {
        FixedString32[] parts = new[]
        {
            new FixedString32("Part1_"),
            new FixedString32("Part2")
        };

        string joined = FixedStringUtils.Join(parts);

        Assert.AreEqual(11, joined.Length);
        Assert.AreEqual("Part1_Part2", joined);
    }

    [Test]
    public void Utils_CutAndJoin_EmptyString_RoundTripsToEmpty()
    {
        string input = "";
        FixedString32[] parts = FixedStringUtils.Cut(input, s => new FixedString32(s));

        Assert.AreEqual(1, parts.Length); // Should always return at least one part, even if it's empty

        string joined = FixedStringUtils.Join(parts);
        Assert.AreEqual(input, joined);
    }

    [Test]
    public void ComparisonsWithGarbage_WorksCorrectly()
    {
        byte[] data1 = new byte[] { /* Size */ 2, 0, /* Contents */ 65, 0, 66, 0, 5, 6, 7, 8 };
        byte[] data2 = new byte[] { /* Size */ 2, 0, /* Contents */ 65, 0, 66, 0, 0, 0, 0, 0 };

        FixedString8 fstr1 = Binary<FixedString8>.Deserialize(data1);
        FixedString8 fstr2 = Binary<FixedString8>.Deserialize(data2);

        Assert.AreEqual(fstr1, fstr2);
        Assert.That(fstr1.ToString(), Is.EqualTo("AB"));
    }
}
