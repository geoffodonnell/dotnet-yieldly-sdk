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

		private const string mNoLossLotteryAppName = "no_loss_lottery_app";
		private const string mStakingAppName = "staking_app";
		private const string mOptingAppName = "opting_app";
		private const string mTrackerAppName = "tracker_app";
		private const string mRateAppName = "rate_app";
		private const string mProxyAppName = "proxy_app";
		private const string mEscrowAppName = "escrow_app";
		private const string mSignatoriesAppName = "signatories_app";
		private const string mValidatorsAppName = "validators_app";
		private const string mDispatcherAppName = "dispatcher_app";
		private const string mBridgeOptingAppName = "bridge_opting_app";
		private const string mBridgeProxyAppName = "bridge_proxy_app";

		private const string mEscrowLogicSigName = "escrow_logicsig";

		private const string mResourceFileLoadErrorFormat =
			"An error occured while loading the embedded resource file: '{0}'";

		private static ContractCollection mContracts;

		private static AppContract mNoLossLotteryApp;
		private static AppContract mStakingApp;
		private static AppContract mOptingApp;
		private static AppContract mTrackerApp;
		private static AppContract mRateApp;
		private static AppContract mProxyApp;
		private static AppContract mEscrowApp;
		private static AppContract mSignatoriesApp;
		private static AppContract mValidatorsApp;
		private static AppContract mDispatcherApp;
		private static AppContract mBridgeOptingApp;
		private static AppContract mBridgeProxyApp;

		private static LogicSigContract mEscrowLogicSig;

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
#if DEBUG
			mNoLossLotteryApp = mContracts.Contracts[mNoLossLotteryAppName] as AppContract;
			mStakingApp = mContracts.Contracts[mStakingAppName] as AppContract;
			mOptingApp = mContracts.Contracts[mOptingAppName] as AppContract;
			mTrackerApp = mContracts.Contracts[mTrackerAppName] as AppContract;
			mRateApp = mContracts.Contracts[mRateAppName] as AppContract;
			mProxyApp = mContracts.Contracts[mProxyAppName] as AppContract;
			mEscrowApp = mContracts.Contracts[mEscrowAppName] as AppContract;
			mSignatoriesApp = mContracts.Contracts[mSignatoriesAppName] as AppContract;
			mValidatorsApp = mContracts.Contracts[mValidatorsAppName] as AppContract;
			mDispatcherApp = mContracts.Contracts[mDispatcherAppName] as AppContract;
			mBridgeOptingApp = mContracts.Contracts[mBridgeOptingAppName] as AppContract;
			mBridgeProxyApp = mContracts.Contracts[mBridgeProxyAppName] as AppContract;
			mEscrowLogicSig = mContracts.Contracts[mEscrowLogicSigName] as LogicSigContract;
#endif
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
