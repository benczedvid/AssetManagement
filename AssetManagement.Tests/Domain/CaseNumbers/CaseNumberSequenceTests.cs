using System;
using System.Reflection;
using AssetManagement.Domain.Entities.CaseNumbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AssetManagement.Tests.Domain.Entities.CaseNumbers;

[TestClass]
public sealed class CaseNumberSequenceTests
{
    [TestMethod]
    public void Create_WithValidArguments_SetsInitialValues()
    {
        // Act
        var sequence = CaseNumberSequence.Create("CASE", 2026);

        // Assert
        Assert.AreEqual("CASE", sequence.Prefix);
        Assert.AreEqual(2026, sequence.Year);
        Assert.AreEqual(0, sequence.LastNumber);
    }

    [TestMethod]
    [DataRow("case", "CASE")]
    [DataRow("  case  ", "CASE")]
    [DataRow("\tcase\r\n", "CASE")]
    [DataRow("cAsE123", "CASE123")]
    [DataRow("123", "123")]
    [DataRow("a", "A")]
    [DataRow("abcdefghij", "ABCDEFGHIJ")]
    [DataRow("  abcdefghij  ", "ABCDEFGHIJ")]
    [DataRow("ügy123", "ÜGY123")]
    public void Create_WithValidPrefix_NormalizesPrefix(string prefix, string expectedPrefix)
    {
        // Act
        var sequence = CaseNumberSequence.Create(prefix, 2026);

        // Assert
        Assert.AreEqual(expectedPrefix, sequence.Prefix);
    }

    [TestMethod]
    public void Create_WithNullPrefix_ThrowsArgumentNullException()
    {
        // Act
        var exception = Assert.ThrowsExactly<ArgumentNullException>(() => CaseNumberSequence.Create(null!, 2026));

        // Assert
        Assert.AreEqual("prefix", exception.ParamName);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\r\n")]
    [DataRow("\t \r\n")]
    public void Create_WithEmptyOrWhitespacePrefix_ThrowsArgumentException(string prefix)
    {
        // Act
        var exception = Assert.ThrowsExactly<ArgumentException>(() => CaseNumberSequence.Create(prefix, 2026));

        // Assert
        Assert.AreEqual("prefix", exception.ParamName);
    }

    [TestMethod]
    [DataRow("ABCDEFGHIJK")]
    [DataRow("12345678901")]
    [DataRow("  ABCDEFGHIJK  ")]
    public void Create_WithPrefixLongerThanTenCharacters_ThrowsArgumentException(string prefix)
    {
        // Act
        var exception = Assert.ThrowsExactly<ArgumentException>(() => CaseNumberSequence.Create(prefix, 2026));

        // Assert
        Assert.AreEqual("prefix", exception.ParamName);
        StringAssert.StartsWith(exception.Message,
            "The case number prefix cannot exceed 10 characters.");
    }

    [TestMethod]
    [DataRow("CASE-1")]
    [DataRow("CASE_1")]
    [DataRow("CASE 1")]
    [DataRow("CASE.1")]
    [DataRow("CASE/1")]
    [DataRow("CASE\\1")]
    [DataRow("CASE!")]
    [DataRow("CA\tSE")]
    [DataRow("CA\nSE")]
    public void Create_WithNonAlphanumericPrefix_ThrowsArgumentException(string prefix)
    {
        // Act
        var exception = Assert.ThrowsExactly<ArgumentException>(() => CaseNumberSequence.Create(prefix, 2026));

        // Assert
        Assert.AreEqual("prefix", exception.ParamName);
        StringAssert.StartsWith(exception.Message,
            "The case number prefix can only contain letters and digits.");
    }

    [TestMethod]
    [DataRow(2000)]
    [DataRow(2026)]
    [DataRow(9999)]
    public void Create_WithValidYear_PreservesYear(int year)
    {
        // Act
        var sequence = CaseNumberSequence.Create("CASE", year);

        // Assert
        Assert.AreEqual(year, sequence.Year);
    }

    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1999)]
    [DataRow(10000)]
    [DataRow(int.MaxValue)]
    public void Create_WithInvalidYear_ThrowsArgumentOutOfRangeException(int year)
    {
        // Act
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CaseNumberSequence.Create("CASE", year));

        // Assert
        Assert.AreEqual("year", exception.ParamName);
        Assert.AreEqual((object)year, exception.ActualValue);
        StringAssert.StartsWith(exception.Message,
            "The case number year must be between 2000 and 9999.");
    }

    [TestMethod]
    public void GetNextNumber_WhenCalledFirstTime_ReturnsOne()
    {
        // Arrange
        var sequence = CaseNumberSequence.Create("CASE", 2026);

        // Act
        var result = sequence.GetNextNumber();

        // Assert
        Assert.AreEqual(1, result);
        Assert.AreEqual(1, sequence.LastNumber);
    }

    [TestMethod]
    public void GetNextNumber_WhenCalledRepeatedly_IncrementsLastNumber()
    {
        // Arrange
        var sequence = CaseNumberSequence.Create("CASE", 2026);

        // Act and assert
        for (var expected = 1; expected <= 100; expected++)
        {
            var result = sequence.GetNextNumber();

            Assert.AreEqual(expected, result);
            Assert.AreEqual(expected, sequence.LastNumber);
        }
    }

    [TestMethod]
    public void GetNextNumber_DoesNotChangePrefixOrYear()
    {
        // Arrange
        var sequence = CaseNumberSequence.Create("CASE", 2026);

        // Act
        sequence.GetNextNumber();

        // Assert
        Assert.AreEqual("CASE", sequence.Prefix);
        Assert.AreEqual(2026, sequence.Year);
    }

    [TestMethod]
    public void GetNextNumber_WithSeparateInstances_MaintainsIndependentCounters()
    {
        // Arrange
        var first = CaseNumberSequence.Create("CASE", 2026);
        var second = CaseNumberSequence.Create("CASE", 2026);

        // Act
        first.GetNextNumber();
        first.GetNextNumber();
        var secondResult = second.GetNextNumber();

        // Assert
        Assert.AreEqual(2, first.LastNumber);
        Assert.AreEqual(1, second.LastNumber);
        Assert.AreEqual(1, secondResult);
    }

    [TestMethod]
    public void GetNextNumber_WhenBelowMaximum_ReturnsMaximum()
    {
        // Arrange
        var sequence = CaseNumberSequence.Create("CASE", 2026);
        SetLastNumber(sequence, int.MaxValue - 1);

        // Act
        var result = sequence.GetNextNumber();

        // Assert
        Assert.AreEqual(int.MaxValue, result);
        Assert.AreEqual(int.MaxValue, sequence.LastNumber);
    }

    [TestMethod]
    public void GetNextNumber_WhenAtMaximum_ThrowsAndPreservesLastNumber()
    {
        // Arrange
        var sequence = CaseNumberSequence.Create("CASE", 2026);
        SetLastNumber(sequence, int.MaxValue);

        // Act
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => sequence.GetNextNumber());

        // Assert
        Assert.AreEqual("The case number sequence has reached its maximum value.", exception.Message);
        Assert.AreEqual(int.MaxValue, sequence.LastNumber);
    }

    [TestMethod]
    public void GetNextNumber_AfterReachingMaximum_ThrowsOnNextCall()
    {
        // Arrange
        var sequence = CaseNumberSequence.Create("CASE", 2026);
        SetLastNumber(sequence, int.MaxValue - 1);

        // Act
        var lastValidNumber = sequence.GetNextNumber();

        // Assert
        Assert.AreEqual(int.MaxValue, lastValidNumber);
        Assert.ThrowsExactly<InvalidOperationException>(() => sequence.GetNextNumber());
        Assert.AreEqual(int.MaxValue, sequence.LastNumber);
    }

    private static void SetLastNumber(CaseNumberSequence sequence, int lastNumber)
    {
        var property = typeof(CaseNumberSequence).GetProperty(
            nameof(CaseNumberSequence.LastNumber),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.IsNotNull(property);

        var setter = property.GetSetMethod(nonPublic: true);

        Assert.IsNotNull(setter);

        setter.Invoke(sequence, new object[] { lastNumber });
    }
}