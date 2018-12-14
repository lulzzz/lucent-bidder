using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Lucent.Common.Entities;
using Lucent.Common.Filters;
using Lucent.Common.OpenRTB;
using Lucent.Common.Serialization;
using Lucent.Common.Test;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lucent.Common.Storage.Test
{
    [TestClass]
    public class FilterTests : BaseTestClass
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        protected override void InitializeDI(IServiceCollection services)
        {
            services.AddLucentServices(Configuration, localOnly: true);
        }

        bool BaseLine(BidRequest bid)
        {
            if (bid.Impressions != null)
            {
                foreach (var imp in bid.Impressions)
                {
                    if (imp.BidCurrency == "CAN")
                        return true;
                }
            }

            if (bid.User != null && (bid.User.GenderStr == "U" ||
                (bid.User.Geo != null && bid.User.Geo.Country == "CAN")))
                return true;

            return false;
        }


        [TestMethod]
        public void TestBidFilter()
        {
            var req = new BidRequest
            {
                Impressions = new Impression[] { new Impression { ImpressionId = "test" } },
                User = new User { Gender = Gender.Male }
            };

            var bFilter = new BidFilter
            {
                ImpressionFilters = new[] { new Filter { Property = "BidCurrency", Value = "CAN" } },
                UserFilters = new[] { new Filter { Property = "GenderStr", Value = "U" } },
                GeoFilters = new[] { new Filter { FilterType = FilterType.EQ, Property = "Country", Value = "CAN" } }
            };


            var f = bFilter.GenerateCode();

            Assert.IsFalse(f.Invoke(req), "Filter should not have matched");

            req.User.Geo = new Geo { Country = "CAN" };

            Assert.IsTrue(f.Invoke(req), "Filter should have matched");
        }

        [TestMethod]
        public void TestInFilter()
        {
            var req = new BidRequest
            {
                Impressions = new Impression[] { new Impression { ImpressionId = "test" } },
                User = new User { Gender = Gender.Male },
                Site = new Site { SiteCategories = new string[] { "BCAT1" }, Domain = "lucentbid.com" }
            };

            var bFilter = new BidFilter
            {
                SiteFilters = new[] { new Filter { FilterType = FilterType.IN, Property = "SiteCategories", Values = new FilterValue[] { "BCAT1" } },
                    new Filter { FilterType = FilterType.IN, Property = "SiteCategories", Value = "BCAT3" }, new Filter { FilterType = FilterType.IN, Property = "Domain", Values = new FilterValue[]{"telefrek.com", "telefrek.co", "bad" }} },
            };

            var f = bFilter.GenerateCode();

            Assert.IsTrue(f.Invoke(req), "Bid should have been filtered");

            req.Site.SiteCategories = new string[] { "BCAT2" };
            Assert.IsFalse(f.Invoke(req), "Bid should not have been filtered");

            req.Site.SiteCategories = new string[] { "BCAT2", "BCAT3" };
            Assert.IsTrue(f.Invoke(req), "Bid should have been filtered");


            req.Site.SiteCategories = new string[] { "BCAT2" };
            req.Site.Domain = "telefrek.co";
            Assert.IsTrue(f.Invoke(req), "Bid should have been filtered");

            req.Site.Domain = "adobada";
            Assert.IsTrue(f.Invoke(req), "Bid should have been filtered");

            req.Site.Domain = null;
            Assert.IsFalse(f.Invoke(req), "Bid should not have been filtered");

            req.Site.SiteCategories = new string[] { "BCAT1" };
            Assert.IsTrue(f.Invoke(req), "Bid should have been filtered");
        }
    }
}