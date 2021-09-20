using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Yieldly.V1.Asc {

	internal class ContractConverter : JsonConverter {
		
		private static readonly string mTypeKey = "type";
		private static readonly ConcurrentDictionary<string, Type> mTypes
			= new ConcurrentDictionary<string, Type>();

		static ContractConverter() {
			var contractTypes = typeof(Contract)
				.Assembly
				.GetTypes()
				.Where(s => s.GetCustomAttribute<ContractTypeAttribute>() != null)
				.ToArray();

			foreach (var contractType in contractTypes) {
				var type = contractType.GetCustomAttribute<ContractTypeAttribute>()?.Type;

				if (!String.IsNullOrWhiteSpace(type)) {
					mTypes.TryAdd(type, contractType);
				}
			}
		}

		public override object ReadJson(
			JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {

			var jObject = JObject.Load(reader);
			string typeName = null;

			if (jObject.TryGetValue(
					mTypeKey, StringComparison.OrdinalIgnoreCase, out var value)) {

				typeName = value.ToString();
			}

			var actualType = String.IsNullOrWhiteSpace(typeName) ?
				objectType :
				mTypes[typeName];

			var resolver = serializer.ContractResolver;
			var contract = resolver.ResolveContract(actualType) as JsonObjectContract;

			var result = contract.DefaultCreator();

			foreach (var property in contract
				.Properties
				.Where(s => !s.Ignored && s.Writable)) {

				var propertyName = property.PropertyName;
				var propertyType = property.PropertyType;
				var converter = property.Converter;

				if (!jObject.TryGetValue(propertyName, out var propertyToken)) {
					continue;
				}

				object propertyValue = null;

				if (converter != null) {
					propertyValue = converter.ReadJson(propertyToken.CreateReader(), propertyType, null, serializer);
				} else {
					propertyValue = propertyToken.ToObject(propertyType, serializer);
				}

				if (propertyValue != null) {
					property.ValueProvider.SetValue(result, propertyValue);
				}
			}

			return result;
		}

		public override void WriteJson(
			JsonWriter writer, object value, JsonSerializer serializer) {

			if (value == null) {
				writer.WriteNull();
				return;
			}

			var resolver = serializer.ContractResolver;
			var contract = resolver.ResolveContract(value.GetType()) as JsonObjectContract;

			writer.WriteStartObject();

			foreach (var property in contract.Properties) {
				if (property.Readable && !property.Ignored) {
					var converter = property.Converter;
					var propertyValue = property.ValueProvider.GetValue(value);

					writer.WritePropertyName(property.PropertyName);

					if (converter != null) {
						converter.WriteJson(writer, propertyValue, serializer);
					} else {
						serializer.Serialize(writer, propertyValue);
					}
				}
			}

			writer.WriteEndObject();
		}

		public override bool CanConvert(Type objectType) {
			return typeof(Contract).IsAssignableFrom(objectType);
		}

	}

}
