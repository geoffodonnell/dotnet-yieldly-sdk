using Algorand;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.IO;
using Yieldly.V1.Asc;

namespace Yieldly.V1 {

	public static class Contract {

		private const string mResourceFileName = "Yieldly.V1.asc.json";
		private const string mEscrowLogicSigName = "escrow_logicsig";
		private const string mAsaStakePoolLogicSigName = "asa_stake_pool_logicsig";
		private const string mResourceFileLoadErrorFormat =
			"An error occured while loading the embedded resource file: '{0}'";

		private static ContractCollection mContracts;
		private static LogicSigContract mEscrowLogicSig;
		private static LogicSigContract mAsaStakePoolLogicSigDef;
		private static LogicsigSignature mEscrowLogicsigSignature;

		private static readonly object mLock = new object();

		public static LogicsigSignature EscrowLogicsigSignature {
			get {
				if (mEscrowLogicsigSignature == null) {
					Initialize();
				}

				return mEscrowLogicsigSignature;
			}
		}

		public static LogicsigSignature GetAsaStakePoolLogicsigSignature(
			ulong appId, bool usePatch = true) {

			Initialize();

			var bytes = YieldlyUtils.GetProgram(
				mAsaStakePoolLogicSigDef.Logic,
				new Dictionary<string, object> {
					{ "app_id", appId }
				});

			return GetLogicsigSignature(bytes, usePatch);
		}

		private static void Initialize() {

			if (mContracts != null) {
				return;
			}

			lock (mLock) {
				if (mContracts == null) {
					LoadContractsFromResource();
				}
			}
		}

		private static void LoadContractsFromResource() {

			var assembly = typeof(Contract).Assembly;
			var serializer = JsonSerializer.CreateDefault();

			var stream = assembly.GetManifestResourceStream(mResourceFileName);

			if (stream == null) {
				throw new Exception(
					String.Format(mResourceFileLoadErrorFormat, mResourceFileName));
			}

			try {
				using (var reader = new StreamReader(stream))
				using (var json = new JsonTextReader(reader)) {
					mContracts = serializer.Deserialize<ContractCollection>(json);
				}
			} catch (Exception ex) {
				throw new Exception(
					String.Format(mResourceFileLoadErrorFormat, mResourceFileName), ex);
			}

			stream.Dispose();

			mEscrowLogicSig = mContracts.Contracts[mEscrowLogicSigName] as LogicSigContract;
			mAsaStakePoolLogicSigDef = mContracts.Contracts[mAsaStakePoolLogicSigName] as LogicSigContract;
			mEscrowLogicsigSignature = GetLogicsigSignature(Base64.Decode(mEscrowLogicSig?.Logic?.ByteCode), true);
		}

		private static LogicsigSignature GetLogicsigSignature(byte[] bytes, bool usePatch) {

			if (TryCreateLogicsigSignature(bytes, out var result, out var exception)) {
				return result;
			}

			if (usePatch && TryCreateLogicsigSignatureWithPatch(bytes, out result, out exception)) {
				return result;
			}

			throw exception;
		}

		private static bool TryCreateLogicsigSignature(
			byte[] bytes, out LogicsigSignature result, out Exception exception) {

			try {

				result = new LogicsigSignature(logic: bytes);
				exception = null;

				return true;

			} catch (Exception ex) {

				result = null;
				exception = ex;

				return false;
			}
		}

		private static bool TryCreateLogicsigSignatureWithPatch(
			byte[] bytes, out LogicsigSignature result, out Exception exception) {

			try {

				result = new LogicsigSignature {
					logic = bytes
				};

				Patch.Logic.CheckProgram(bytes, null);

				exception = null;

				return true;

			} catch (Exception ex) {

				result = null;
				exception = ex;

				return false;
			}
		}

	}

}
