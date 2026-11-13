using NUnit.Framework;
using WobbleBrawl.Gameplay;

public class DamageResolverTests
{
    [TestCase(HurtZone.Body, false, false, false, 10)]
    [TestCase(HurtZone.Legs, false, false, false, 5)]
    [TestCase(HurtZone.Body, true, false, false, 3)]
    [TestCase(HurtZone.Body, true, true, false, 5)]
    [TestCase(HurtZone.Body, true, true, true, 0)]
    public void DamageResolverAppliesZoneAndDefense(HurtZone zone, bool block, bool special, bool immune, int expected)
        => Assert.That(DamageResolver.Resolve(zone, 10, 5, 100, 10, 10, 0, 0, block, special, immune).Damage,
            Is.EqualTo(expected));

    [Test]
    public void DamageResolverBreaksGuardAndCapsComboStun()
    {
        var broken = DamageResolver.Resolve(HurtZone.Body, 10, 5, 10, 10, 10, 0, 0, true, false, false);
        Assert.That(broken.GuardBroken, Is.True);
        Assert.That(broken.StunFrames, Is.EqualTo(60));
        var combo = DamageResolver.Resolve(HurtZone.Body, 10, 5, 100, 10, 10, 4, 29, false, false, false);
        Assert.That(combo.Damage, Is.EqualTo(6));
        Assert.That(combo.StunFrames, Is.EqualTo(1));
    }
}
