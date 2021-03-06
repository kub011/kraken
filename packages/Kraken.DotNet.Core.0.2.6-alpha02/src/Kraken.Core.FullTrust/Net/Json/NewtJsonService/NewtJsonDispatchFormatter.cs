﻿/* There's no .net 3.5 equivalent for System.Net.Http.Formatting
 * at least that we can find so far, so for now folks in order 
 * versions will just have to do without.
 */
#if !DOTNET_V35
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml;

using newt = Newtonsoft.Json;

namespace Kraken.Net.WebApi {

	public class NewtJsonDispatchFormatter : IDispatchMessageFormatter {
		OperationDescription operation;
		Dictionary<string, int> parameterNames;

		public NewtJsonDispatchFormatter(OperationDescription operation, bool isRequest) {
			this.operation = operation;
			if (isRequest) {
				int operationParameterCount = operation.Messages[0].Body.Parts.Count;
				if (operationParameterCount > 1) {
					this.parameterNames = new Dictionary<string, int>();
					for (int i = 0; i < operationParameterCount; i++) {
						this.parameterNames.Add(operation.Messages[0].Body.Parts[i].Name, i);
					}
				}
			}
		}

		public void DeserializeRequest(Message message, object[] parameters) {
			object bodyFormatProperty;
			if (!message.Properties.TryGetValue(WebBodyFormatMessageProperty.Name, out bodyFormatProperty) ||
				(bodyFormatProperty as WebBodyFormatMessageProperty).Format != WebContentFormat.Raw) {
				throw new InvalidOperationException("Incoming messages must have a body format of Raw. Is a ContentTypeMapper set on the WebHttpBinding?");
			}

			var bodyReader = message.GetReaderAtBodyContents();
			bodyReader.ReadStartElement("Binary");
			byte[] rawBody = bodyReader.ReadContentAsBase64();
			var ms = new MemoryStream(rawBody);

			var sr = new StreamReader(ms);
			var serializer =
				RestSharpJsonNetSerializer.NewtonSerializer;
				//new newt.JsonSerializer();
			if (parameters.Length == 1) {
				// single parameter, assuming bare
				parameters[0] = serializer.Deserialize(sr, operation.Messages[0].Body.Parts[0].Type);
			} else {
				// multiple parameter, needs to be wrapped
				newt.JsonReader reader = new newt.JsonTextReader(sr);
				reader.Read();
				if (reader.TokenType != newt.JsonToken.StartObject) {
					throw new InvalidOperationException("Input needs to be wrapped in an object");
				}

				reader.Read();
				while (reader.TokenType == newt.JsonToken.PropertyName) {
					var parameterName = reader.Value as string;
					reader.Read();
					if (this.parameterNames.ContainsKey(parameterName)) {
						int parameterIndex = this.parameterNames[parameterName];
						parameters[parameterIndex] = serializer.Deserialize(reader, this.operation.Messages[0].Body.Parts[parameterIndex].Type);
					} else {
						reader.Skip();
					}

					reader.Read();
				}

				reader.Close();
			}

			sr.Close();
			ms.Close();
		}

		public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result) {
			byte[] body;
			var serializer = new newt.JsonSerializer();

			using (var ms = new MemoryStream()) {
				using (var sw = new StreamWriter(ms, Encoding.UTF8)) {
					using (newt.JsonWriter writer = new newt.JsonTextWriter(sw)) {
						//writer.Formatting = newt.Formatting.Indented;
						serializer.Serialize(writer, result);
						sw.Flush();
						body = ms.ToArray();
					}
				}
			}

			System.ServiceModel.Channels.Message replyMessage = System.ServiceModel.Channels.Message.CreateMessage(messageVersion, operation.Messages[1].Action, new RawBodyWriter(body));
			replyMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Raw));
			var respProp = new HttpResponseMessageProperty();
			respProp.Headers[HttpResponseHeader.ContentType] = "application/json";
			replyMessage.Properties.Add(HttpResponseMessageProperty.Name, respProp);
			return replyMessage;
		}
	}

}
 #endif