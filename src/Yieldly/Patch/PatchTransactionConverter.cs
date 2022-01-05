using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Yieldly.Patch {

	/// <summary>
	/// Performs serialization of <see cref="PatchTransaction"/> objects.
	/// </summary>
	/// <remarks>
	/// This class is deprecated and will be removed in a future release.
	/// </remarks>
	[Obsolete]
	public class PatchTransactionConverter : JsonConverter {

		public override bool CanConvert(Type objectType) {
			return objectType == typeof(PatchTransaction);
		}

		public override object ReadJson(
			JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			
			var resolver = serializer.ContractResolver;
			var contract = resolver.ResolveContract(objectType) as JsonObjectContract;
			var creator = contract.OverrideCreator;
			var jToken = JToken.ReadFrom(reader);
			var jObject = jToken as JObject;

			object result = null;

			if (creator != null) {
				var args = GetCreatorArgs(contract, jObject);
				result = creator(args);
			} else {
				result = contract.DefaultCreator();
			}

			var properties = GetReadableProperties(serializer, result);
			
			foreach (var property in properties) {
				if (jObject.TryGetValue(property.PropertyName, out var token)) {
					var value = token.ToObject(property.PropertyType, serializer);
					property.ValueProvider.SetValue(result, value);
				}
			}

			return result;
		}

		protected virtual object[] GetCreatorArgs(JsonObjectContract contract, JObject jObject) {

			var result = new List<object>();
			var parameters = contract.CreatorParameters;

			foreach (var parameter in parameters) {

				object value = null;

				if (jObject.TryGetValue(parameter.PropertyName, out var jToken)) {
					value = jToken.ToObject(parameter.PropertyType);
					jObject.Remove(parameter.PropertyName);
				} else {
					value = null;
				}

				result.Add(value);
			}

			return result.ToArray();
		}

		protected virtual IList<JsonProperty> GetReadableProperties(
			JsonSerializer serializer, object value) {

			var resolver = serializer.ContractResolver;
			var contract = resolver.ResolveContract(value.GetType()) as JsonObjectContract;

			if (contract == null) {
				return null;
			}

			var result = new List<JsonProperty>();

			foreach (var member in contract.Properties) {
				var shouldDeserialize = member.ShouldDeserialize != null ?
					member.ShouldDeserialize(value) : true;

				if (shouldDeserialize && !member.Ignored) {
					result.Add(member);
				}
			}

			return result;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {

			writer.WriteStartObject();

			var properties = GetWritableProperties(serializer, value);

			foreach (var prop in properties) {
				WriteJsonMemberValue(serializer, writer, prop, value);
			}

			writer.WriteEndObject();
		}

		protected virtual IList<JsonProperty> GetWritableProperties(
			JsonSerializer serializer, object value) {

			var resolver = serializer.ContractResolver;
			var contract = resolver.ResolveContract(value.GetType()) as JsonObjectContract;

			if (contract == null) {
				return null;
			}

			var result = new List<JsonProperty>();

			foreach (var member in contract.Properties) {
				var shouldSerialize = member.ShouldSerialize != null ?
					member.ShouldSerialize(value) : true;

				if (shouldSerialize && !member.Ignored) {
					result.Add(member);
				}
			}

			return result;
		}

		protected virtual void WriteJsonMemberValue(
			JsonSerializer serializer, JsonWriter writer, JsonProperty member, object target) {

			var value = member.ValueProvider.GetValue(target);
			var converters = new List<JsonConverter>() {
				member.Converter,
				member.ItemConverter
			};

			if (serializer.DefaultValueHandling == DefaultValueHandling.Ignore
				|| member.DefaultValueHandling == DefaultValueHandling.Ignore) {

				if (value == null ||
					value.Equals(member.DefaultValue) ||
					(IsNumericType(member.PropertyType) && Convert.ToUInt64(value) == 0) ||
					(IsNullableNumericType(member.PropertyType) && Convert.ToUInt64(value) == 0)) {

					return;
				}
			}

			// --- PATCH
			if (String.Equals(member.PropertyName, "apas", StringComparison.OrdinalIgnoreCase)) {
				var asSdkType = value as List<long>;

				// If the value is null here something has gone sideways
				if (asSdkType != null) {
					value = asSdkType
						.Where(s => s > 0)
						.Select(s => Convert.ToUInt64(s))
						.ToList();
				}
			}
			// --- /PATCH

			writer.WritePropertyName(member.PropertyName);

			var converter = converters
				.Where(s => s != null)
				.FirstOrDefault(s => s.CanConvert(value.GetType()) && s.CanWrite);

			if (converter != null) {
				converter.WriteJson(writer, value, serializer);
			} else {
				serializer.Serialize(writer, value);
			}
		}

		protected static bool IsNullableNumericType(Type type) {

			var underlyingType = Nullable.GetUnderlyingType(type);

			if (underlyingType == null) {
				return false;
			}

			return IsNumericType(underlyingType);
		}

		//https://stackoverflow.com/a/1750093
		protected static bool IsNumericType(Type type) {
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

	}

}
