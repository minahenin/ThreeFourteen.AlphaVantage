﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThreeFourteen.AlphaVantage.Response;
using ThreeFourteen.AlphaVantage.Service;

namespace ThreeFourteen.AlphaVantage.Builders
{
    public abstract class BuilderBase
    {
        private readonly IAlphaVantageService _service;
        private readonly Dictionary<string, string> _fields = new Dictionary<string, string>();

        protected abstract string[] RequiredFields { get; }

        protected abstract Function Function { get; }

        protected BuilderBase(IAlphaVantageService service)
        {
            _service = service;
        }

        internal void SetField(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException(nameof(key));
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(nameof(value));

            _fields[key] = value;
        }

        public Task<string> GetRawDataAsync()
        {
            if (Function != null)
            {
                SetField(ParameterFields.Function, Function.Value);
            }

            var required = new HashSet<string>(RequiredFields);
            required.RemoveWhere(x => _fields.ContainsKey(x));
            if (required.Count > 0)
            {
                throw new InvalidOperationException($"Missing required fields: {string.Join(", ", required)}");
            }

            return _service.GetRawDataAsync(_fields);
        }

        protected virtual async Task<Result<T>> GetDataAsync<T>(Func<JToken, IEnumerable<T>> parseData)
        {
            var res = await GetRawDataAsync();
            JToken node = JToken.Parse(res);

            var errorNode = (node as JObject)?.Properties()?.FirstOrDefault(x => x.Name == "Error Message");
            if (errorNode != null)
            {
                throw new AlphaVantageException(errorNode.Value.Value<string>());
            }

            var informationNode = (node as JObject)?.Properties()?.FirstOrDefault(x => x.Name == "Information");
            if (informationNode != null)
            {
                throw new AlphaVantageException(informationNode.Value.Value<string>());
            }

            var metadataNode = node.Root["Meta Data"];
            if (metadataNode == null)
            {
                throw new AlphaVantageException("Result does not seem to be valid");
            }
            var metadata = ParseMetaData(metadataNode);

            var dataNode = node.Root.Skip(1).FirstOrDefault();
            if (dataNode == null)
            {
                throw new AlphaVantageException("Result does not seem to be valid (missing Data)");
            }
            var data = parseData(dataNode).ToArray();

            return new Result<T>(metadata, data);
        }

        private Metadata ParseMetaData(JToken token)
        {
            var data = new Dictionary<string, string>();
            foreach (var metaData in token.Children<JProperty>())
            {
                var key = metaData.Name.Substring(3);
                var val = metaData.Value.Value<string>();
                data.Add(key, val);
            }

            return new Metadata(data);
        }
    }
}
