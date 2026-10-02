using GenesisUI.Foundation;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class SharedHideClaimsTests
    {
        [Fact]
        public void Either_release_order_preserves_the_other_hidden_owner()
        {
            foreach (bool reverse in new[]{false, true})
            {
                var claims = new SharedHideClaims();
                int a = claims.Acquire(), b = claims.Acquire();
                claims.SetHidden(a, true, null);
                claims.SetHidden(b, true, false);
                claims.Release(reverse ? b : a);
                Assert.True(claims.Hidden);
                Assert.Equal(reverse ? null : (bool?)false, claims.Interactable);
                claims.Release(reverse ? a : b);
                Assert.False(claims.Hidden);
                Assert.Equal(0, claims.Count);
            }
        }

        [Fact]
        public void Stale_releases_and_lifted_veil_do_not_release_another_owner()
        {
            var claims = new SharedHideClaims();
            int a = claims.Acquire(), b = claims.Acquire();
            claims.SetHidden(a, true, false);
            claims.SetHidden(b, true, null);
            claims.SetHidden(a, false, false);
            claims.Release(a);
            int c = claims.Acquire();
            claims.Release(a);
            Assert.Equal(2, claims.Count);
            Assert.True(claims.Hidden);
            claims.Release(b);
            Assert.False(claims.Hidden);
            Assert.Equal(1, claims.Count);
            claims.Release(c);
        }
    }
}
