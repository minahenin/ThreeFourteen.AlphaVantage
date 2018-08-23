﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreeFourteen.AlphaVantage.Response;
using ThreeFourteen.AlphaVantage.Service;

namespace ThreeFourteen.AlphaVantage.Builders.Stocks
{
    public class TimeSeriesDailyAdjustedBuilder : BuilderBase, IHaveData<TimeSeriesAdjustedEntry>, ICanSetOutputSize
    {
        public TimeSeriesDailyAdjustedBuilder(IAlphaVantageService service, string symbol)
            : base(service)
        {
            SetField(ParameterFields.Symbol, symbol);
        }

        protected override string[] RequiredFields => new[] { ParameterFields.Symbol };

        protected override Function Function => Function.Stocks.TimeSeriesDailyAdjusted;

        public Task<Result<TimeSeriesAdjustedEntry>> GetAsync()
        {
            return GetDataAsync(Parse);
        }

        private IEnumerable<TimeSeriesAdjustedEntry> Parse(JToken token)
        {
            var properties = token as JProperty;
            properties.ValidateName(@"Time Series \(Daily\)");

            return properties.First.Children()
                .Select(x => ((JProperty)x).ToTimeSeriesAdjusted())
                .ToList();
        }
    }
}