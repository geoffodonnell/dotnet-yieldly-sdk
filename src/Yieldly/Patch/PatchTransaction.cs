using Algorand;
using Algorand.V2.Indexer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using Transaction = Algorand.Transaction;

namespace Yieldly.Patch {

    /// <summary>
    /// The purpose of this class is to intercept serialization of the Transaction object. The 
    /// canonical class Algorand.Transaction should serialize the foreignAssets property ("apas") into 
    /// an array of UNSIGNED 64-bit integers, not SIGNED 64-bit integers.
    /// </summary>
    /// <remarks>
    /// This class is deprecated and will be removed in a future release.
    /// </remarks>
    [Obsolete]
    [JsonConverter(typeof(PatchTransactionConverter))]
	public class PatchTransaction : Transaction {

		public static PatchTransaction Create(Transaction transaction) {

			// Note - Serializing/deserializing to JSON didn't work here,
			// byte[] got serialized as a base64 string, but deserialization expected a byte[] from 
			// the reader, instead of a string.

			var msgPack = Algorand.Encoder.EncodeToMsgPack(transaction);

			return Algorand.Encoder.DecodeFromMsgPack<PatchTransaction>(msgPack);
		}

        [JsonConstructor]
        protected PatchTransaction(
            [JsonProperty(PropertyName = "type")] Type type,
            [JsonProperty(PropertyName = "snd")] byte[] sender,
            [JsonProperty(PropertyName = "fee")] ulong? fee,
            [JsonProperty(PropertyName = "fv")] ulong? firstValid,
            [JsonProperty(PropertyName = "lv")] ulong? lastValid,
            [JsonProperty(PropertyName = "note")] byte[] note,
            [JsonProperty(PropertyName = "gen")] String genesisID,
            [JsonProperty(PropertyName = "gh")] byte[] genesisHash,
            [JsonProperty(PropertyName = "lx")] byte[] lease,
            [JsonProperty(PropertyName = "rekey")] byte[] rekeyTo,
            [JsonProperty(PropertyName = "grp")] byte[] group,
            [JsonProperty(PropertyName = "amt")] ulong? amount,
            [JsonProperty(PropertyName = "rcv")] byte[] receiver,
            [JsonProperty(PropertyName = "close")] byte[] closeRemainderTo,
            [JsonProperty(PropertyName = "votekey")] byte[] votePK,
            [JsonProperty(PropertyName = "selkey")] byte[] vrfPK,
            [JsonProperty(PropertyName = "votefst")] ulong? voteFirst,
            [JsonProperty(PropertyName = "votelst")] ulong? voteLast,
            [JsonProperty(PropertyName = "votekd")] ulong? voteKeyDilution,
            [JsonProperty(PropertyName = "apar")] AssetParams assetParams,
            [JsonProperty(PropertyName = "caid")] ulong? assetIndex,
            [JsonProperty(PropertyName = "xaid")] ulong? xferAsset,
            [JsonProperty(PropertyName = "aamt")] ulong? assetAmount,
            [JsonProperty(PropertyName = "asnd")] byte[] assetSender,
            [JsonProperty(PropertyName = "arcv")] byte[] assetReceiver,
            [JsonProperty(PropertyName = "aclose")] byte[] assetCloseTo,
            [JsonProperty(PropertyName = "fadd")] byte[] freezeTarget,
            [JsonProperty(PropertyName = "faid")] ulong? assetFreezeID,
            [JsonProperty(PropertyName = "afrz")] bool freezeState) : this(

				type, new Address(sender), fee, firstValid, lastValid, note,
				genesisID, new Digest(genesisHash), lease, null, new Digest(group),
				amount, new Address(receiver), new Address(closeRemainderTo),
				new ParticipationPublicKey(votePK), new VRFPublicKey(vrfPK),
				voteFirst, voteLast, voteKeyDilution,
				assetParams, assetIndex,
				xferAsset, assetAmount, new Address(assetSender), new Address(assetReceiver),
				new Address(assetCloseTo), new Address(freezeTarget), assetFreezeID, freezeState,
				null, default(long), null, null, null, null, null, default(long), null, null) { }


        protected PatchTransaction(Type type,
                //header fields
                Address sender, ulong? fee, ulong? firstValid, ulong? lastValid, byte[] note,
                string genesisID, Digest genesisHash, byte[] lease, Address rekeyTo, Digest group,
                // payment fields
                ulong? amount, Address receiver, Address closeRemainderTo,
                // keyreg fields
                ParticipationPublicKey votePK, VRFPublicKey vrfPK, ulong? voteFirst, ulong? voteLast,
                // voteKeyDilution
                ulong? voteKeyDilution,
                // asset creation and configuration
                AssetParams assetParams, ulong? assetIndex,
                // asset transfer fields
                ulong? xferAsset, ulong? assetAmount, Address assetSender, Address assetReceiver,
                Address assetCloseTo, Address freezeTarget, ulong? assetFreezeID, bool freezeState,
                // application fields
                List<byte[]> applicationArgs, OnCompletion onCompletion, TEALProgram approvalProgram, List<Address> accounts,
                List<ulong> foreignApps, List<ulong> foreignAssets, StateSchema globalStateSchema, ulong? applicationId,
                StateSchema localStateSchema, TEALProgram clearStateProgram) {

            this.type = type;
            if (sender != null) this.sender = sender;
            else this.sender = new Address();

            if (fee != null) this.fee = fee;
            if (firstValid != null) this.firstValid = firstValid;
            if (lastValid != null) this.lastValid = lastValid;
            if (note != null && note.Length > 0) this.note = note;
            if (genesisID != null) this.genesisID = genesisID;
            if (genesisHash != null) this.genesisHash = genesisHash;
            else this.genesisHash = new Digest();

            if (lease != null && lease.Length > 0) this.lease = lease;

            if (group != null) this.group = group;
            else this.group = new Digest();

            if (amount != null) this.amount = amount;
            if (receiver != null) this.receiver = receiver;
            else this.receiver = new Address();

            if (closeRemainderTo != null) this.closeRemainderTo = closeRemainderTo;
            else this.closeRemainderTo = new Address();

            if (votePK != null) this.votePK = votePK;
            else this.votePK = new ParticipationPublicKey();

            if (vrfPK != null) this.selectionPK = vrfPK;
            else this.selectionPK = new VRFPublicKey();

            if (voteFirst != null) this.voteFirst = voteFirst;
            if (voteLast != null) this.voteLast = voteLast;
            if (voteKeyDilution != null) this.voteKeyDilution = voteKeyDilution;
            if (assetParams != null) this.assetParams = assetParams;
            else this.assetParams = new AssetParams();

            if (assetIndex != null) this.assetIndex = assetIndex;
            if (xferAsset != null) this.xferAsset = xferAsset;
            if (assetAmount != null) this.assetAmount = assetAmount;
            if (assetSender != null) this.assetSender = assetSender;
            else this.assetSender = new Address();

            if (assetReceiver != null) this.assetReceiver = assetReceiver;
            else this.assetReceiver = new Address();

            if (assetCloseTo != null) this.assetCloseTo = assetCloseTo;
            else this.assetCloseTo = new Address();

            if (freezeTarget != null) this.freezeTarget = freezeTarget;
            else this.freezeTarget = new Address();

            if (assetFreezeID != null) this.assetFreezeID = assetFreezeID;
            this.freezeState = freezeState;

            if (rekeyTo != null) this.RekeyTo = rekeyTo;
            if (applicationArgs != null) this.applicationArgs = applicationArgs;
            this.onCompletion = onCompletion;
            if (approvalProgram != null) this.approvalProgram = approvalProgram;
            if (accounts != null) this.accounts = accounts;
            if (foreignApps != null) this.foreignApps = foreignApps;
            if (foreignAssets != null) this.foreignAssets = foreignAssets;
            if (globalStateSchema != null) this.globalStateSchema = globalStateSchema;
            if (applicationId != null) this.applicationId = applicationId;
            if (localStateSchema != null) this.localStateSchema = globalStateSchema;
            if (clearStateProgram != null) this.clearStateProgram = clearStateProgram;
        }

    }

}
