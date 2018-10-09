using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Lucent.Common.Entities;
using Lucent.Common.Entities.Serializers;
using Lucent.Common.Filters;
using Lucent.Common.Filters.Serializers;
using Lucent.Common.OpenRTB;
using Lucent.Common.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Lucent.Common
{
    public static partial class LucentExtensions
    {
        public static IServiceCollection AddEntitySerializers(this IServiceCollection provider)
        {
            var registry = provider.BuildServiceProvider().GetRequiredService<ISerializationRegistry>();
            if (!registry.IsSerializerRegisterred<Campaign>())
            {
                registry.Register<Campaign>(new CampaignSerializer());
                registry.Register<CampaignSchedule>(new CampaignScheduleSerializer());
                registry.Register<Creative>(new CreativeSearializer());
                registry.Register<CreativeContent>(new CreativeContentSerializer());
                registry.Register<Filter>(new FilterSerializer());
                registry.Register<BidFilter>(new BidFilterSerializer());
            }
            return provider;
        }

        public static void HydrateFilter(this CreativeContent content)
        {
            var impParam = Expression.Parameter(typeof(Impression), "imp");
            var fValue = Expression.Variable(typeof(bool), "isFiltered");
            var expressions = new List<Expression>();

            var ret = Expression.Label(typeof(bool)); // We're going to return a bool

            if (content.ContentType == ContentType.Banner)
            {
                // Banner Filters
                var bValue = Expression.Property(impParam, "Banner");

                var bannerExpressions = new List<Expression>();

                var hProp = Expression.Property(bValue, "H");
                var wProp = Expression.Property(bValue, "W");

                var hMin = Expression.Property(bValue, "HMin");
                var hMax = Expression.Property(bValue, "HMax");

                var wMin = Expression.Property(bValue, "WMin");
                var wMax = Expression.Property(bValue, "WMax");

                // If H < hMin || H > hMax
                bannerExpressions.Add(Expression.IfThen(Expression.OrElse(Expression.GreaterThan(hMax, Expression.Constant(content.H)), Expression.LessThan(hMin, Expression.Constant(content.H))), Expression.Label(ret, Expression.Constant(true))));

                // If W < wMin || W > wMax
                bannerExpressions.Add(Expression.IfThen(Expression.OrElse(Expression.GreaterThan(wMax, Expression.Constant(content.W)), Expression.LessThan(wMin, Expression.Constant(content.W))), Expression.Label(ret, Expression.Constant(true))));

                if (content.CanScale)
                {
                    // Test of impression h / w == content h / w
                    bannerExpressions.Add(Expression.IfThen(
                        Expression.Equal(
                            Expression.Divide(
                                Expression.Multiply(
                                    hProp, Expression.Constant(1.0d)),
                                Expression.Multiply(
                                    wProp, Expression.Constant(1.0d))
                            ),
                            Expression.Constant((content.H * 1.0d) / (content.W * 1.0d))
                        ),
                        Expression.Label(ret, Expression.Constant(true))));
                }
                else
                {
                    // Test if content.h/w == imp.h/w
                    bannerExpressions.Add(Expression.IfThen(
                        Expression.OrElse(
                            Expression.NotEqual(
                                hProp, Expression.Constant(content.H)),
                            Expression.NotEqual(
                                wProp, Expression.Constant(content.W))
                        ), Expression.Label(ret, Expression.Constant(true))));
                }

                // Create a block
                var bannerBlock = Expression.Block(bannerExpressions);

                // Execute the block if the banner property is not null
                expressions.Add(Expression.IfThen(Expression.NotEqual(bValue, Expression.Constant(null)), bannerBlock));

                // Add the default return false for the end
                expressions.Add(Expression.Label(ret, Expression.Constant(false)));
            }
            else if (content.ContentType == ContentType.Video)
            {
                // Video Filters
                var vValue = Expression.Property(impParam, "Video");

                expressions.Add(Expression.Label(ret, Expression.Constant(false)));
            }
            else
            {
                expressions.Add(Expression.Label(ret, Expression.Constant(true)));
            }

            // Compile the expression block
            var final = Expression.Block(expressions);

            // Build the method
            var ftype = typeof(Func<,>).MakeGenericType(typeof(Impression), typeof(bool));
            var comp = makeLambda.MakeGenericMethod(ftype).Invoke(null, new object[] { final, new ParameterExpression[] { impParam } });

            // Compile the filter
            content.Filter = (Func<Impression, bool>)comp.GetType().GetMethod("Compile", Type.EmptyTypes).Invoke(comp, new object[] { });

        }
    }
}