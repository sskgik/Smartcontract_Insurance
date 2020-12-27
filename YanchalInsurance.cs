using System;
using Miyabi.Asset.Models;
using Miyabi.Binary.Models;
using Miyabi.Common.Models;
using Miyabi.ContractSdk;
using Miyabi.Contract.Models;
using Miyabi.ModelSdk.Execution;

namespace Miyabi.Tests.SCs
{
    public class P2pInsurance : ContractBase
    {
        static string InsuranceTableName = "YanchalInsurance";              //保険の払い出しテーブル
        static string ParticipantListTableName = "YanchalParticipantList";  //参加者の保険料のテーブル
        public P2pInsurance(ContractInitializationContext ctx) : base(ctx)
        {

        }

        /// <summary>
        /// SmartContract Instance
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool Instantiate(string[] args)
        {
            //Assettableownerkwy = contract admin key 
            var contractAdmin = new[]
            {
                GetContractAddress(),
            };

            var insuranceTableName = GetInsuranceTableName();
            //AssetDiscripter(tablename,tracked ,proof,contractadmin(tableowner))
            var assettableDescriptor = new AssetTableDescriptor(insuranceTableName, false,false,contractAdmin);

            var participantListTableName = GetParticipantListTableName();
            //BinarytableDescriptor(tablename,tracks)
            var binarytableDescriptor = new BinaryTableDescriptor(participantListTableName, false);

            try
            {
                //statewrite is environment hold
                StateWriter.AddTable(assettableDescriptor);
                StateWriter.AddTable(binarytableDescriptor);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// join (particepate Insurance)
        /// </summary>
        /// <param name="PaticipantAddress"></param>
        /// <param name="deposit"></param>
        public void join(Address PaticipantAddress,decimal deposit)
        {
            var participantTableName = GetParticipantListTableName();
            //TryGetTableWriter:(StateWriterに登録されたテーブルがあればtrue)
            if (!StateWriter.TryGetTableWriter<IBinaryTableWriter>(participantTableName,out var participantTable))   //happen false
            {
                return;
            }

            if(participantTable.TryGetValue(PaticipantAddress.Encoded,out var value))
            {
                return;
            }
            //Binary values ​​are set in the participant table
            participantTable.SetValue(PaticipantAddress.Encoded, ByteString.Parse("00"));

            var insuranceTableName = GetInsuranceTableName();
            //TryGetTableWriter:(StateWriterに登録されたテーブルがあればtrue de srue)
            if (!StateWriter.TryGetTableWriter<IAssetTableWriter>(insuranceTableName, out var table))
            {
                return;
            }
            //Insurance payment
            table.MoveValue(PaticipantAddress, GetContractAddress(), deposit); //from(sender):PaticipantAddress, to(destination):GetContractAddress(), deposit(Monthly insurance money):deposit
        }

        /// <summary>
        /// vote method
        /// </summary>
        /// <param name="ParticipantAddress"></param>
        public void vote(Address ParticipantAddress)
        {
            var participantTableName = GetParticipantListTableName();
            if (!StateWriter.TryGetTableWriter<IBinaryTableWriter>(participantTableName, out var participantTable))   //happen false
            {
                return;
            }

            if (!participantTable.TryGetValue(ParticipantAddress.Encoded, out var value))
            {
                return;
            }

            participantTable.SetValue(ParticipantAddress.Encoded, ByteString.Parse("01"));

            //confirm number of participant votes

            var insuranceTableName = GetInsuranceTableName();
            if(!StateWriter.TryGetTableWriter<IAssetTableWriter>(insuranceTableName,out var table))
            {
                return;
            }
            // Can pay insurance amount?
            var contractAddress = GetContractAddress();
            if(!TryGetInternalValue(ByteString.Encode("beneficiaryAddress"), out var address))
            {
                return;
            }

            var beneficiaryAddress = PublicKeyAddress.Decode(address);

            decimal amount = 100m;
            table.MoveValue(contractAddress, beneficiaryAddress, amount);
        }

        /// <summary>
        /// happen method
        /// </summary>
        /// <param name="beneficiaryAddress"></param>
        public void happen(Address beneficiaryAddress)
        {
            SetInternalValue( ByteString.Encode("beneficiaryAddress"),beneficiaryAddress.Encoded);
        }

        /// <summary>
        /// GenerateTest method
        /// </summary>
        /// <param name="participantAddress"></param>
        /// <param name="amount"></param>
        public void GenerateTest(Address participantAddress, decimal amount)
        {
            var insuranceTableName = GetInsuranceTableName();
            if (!StateWriter.TryGetTableWriter<IAssetTableWriter>(insuranceTableName, out var table))
            {
                return;
            }

            table.MoveValue(table.VoidAddress, participantAddress, amount);
        }

        public bool TryGetInternalValue(ByteString key, out ByteString value)
        {
            return InternalTable.TryGetValue(key, out value);
        }

        public void SetInternalValue(ByteString key,ByteString value)
        {
            if(InternalTable is IPermissionedBinaryTableWriter internalTableWriter)
            {
                internalTableWriter.CreateOrUpdateValue(key, value);
            }
            else
            {
                throw new InvalidOperationException("Context is readOnly");
            }
        }

        private string GetInsuranceTableName()
        {
            return AddInstanceSuffix(InsuranceTableName);
        }

        private string GetParticipantListTableName()
        {
            return AddInstanceSuffix(ParticipantListTableName);
        }

        private string AddInstanceSuffix(string tableName)
        {
            return tableName + InstanceName;
        }

        private Address GetContractAddress()
        {
            return ContractAddress.FromInstanceId(InstanceId);
        }
    }
}