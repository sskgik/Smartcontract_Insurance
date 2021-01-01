using Miyabi;
using Miyabi.ClientSdk;
using Miyabi.Common.Models;
using Miyabi.Contract.Client;
using Miyabi.Contract.Models;
using Miyabi.Cryptography;
using System;
using System.IO;
using System.Threading.Tasks;
using Utility;
using System.Diagnostics;

/// <summary>
/// Program.cs deploys, instantiates, invokes and queries a smart contracts.
/// The smart contract it self is sc/sc1.cs
/// </summary>
namespace SmartContractSample
{
    class Program
    {
        const string ContractName = "Miyabi.Tests.SCs.P2pInsurance"; // Name Space + "." + Class name
        const string InstanceName = "SmartContractInstance";
        const string Filename = "sc\\YanchalInsurance.cs";

        static readonly ByteString s_AssemblyId =
            ContractUtils.GetAssemblyId(new[] { File.ReadAllText(Filename) });

        static async Task Main(string[] args)
        {
            var handler = Utils.GetBypassRemoteCertificateValidationHandler();

            var config = new SdkConfig(Utils.ApiUrl);
            var client = new Client(config, handler);

            // Ver2 implements module system. To enable modules, register is required.
            //AssetTypesRegisterer.RegisterTypes();
            ContractTypesRegisterer.RegisterTypes();

           // await DeployContract(client);
           // await InstantiateContract(client);
           await InvokeContract(client);
            //await QueryMethod(client, "Read", new [] { "01" });

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        private static async Task DeployContract(IClient client)
        {
            // General API has SendTransactionAsync
            var generalApi = new GeneralApi(client);

            // Create entry
            var sources = new[] { File.ReadAllText(Filename) };
            var dependencies = new[] { "Miyabi.Binary.Models", "Miyabi.Asset.Models" };
            var instantiators = new[] { new PublicKeyAddress(Utils.GetOwnerKeyPair().PublicKey) };
            var entry = new ContractDeploy(sources, dependencies, instantiators);

            // Create transaction
            var tx = TransactionCreator.CreateTransaction(
                new[] { entry },
                new[] { new SignatureCredential(Utils.GetContractAdminKeyPair().PublicKey) });

            // Sign transaction. To deploy a smart contract, contract admin private key is
            // required
            var txSigned = TransactionCreator.SignTransaction(
                tx,
                new[] { Utils.GetContractAdminKeyPair().PrivateKey });

            // Send transaction
            await generalApi.SendTransactionAsync(txSigned);

            // Wait until the transaction is stored in a block and get the result
            var result = await Utils.WaitTx(generalApi, tx.Id);
            Console.WriteLine($"txid={tx.Id}, result={result}");
        }

        private static async Task InstantiateContract(IClient client)
        {
            var generalApi = new GeneralApi(client);

            // Create gen entry
            var arguments = new[] { "dummy" };
            var entry = new ContractInstantiate(s_AssemblyId, ContractName, InstanceName, arguments);

            // Create signed transaction with builder. To generate instantiate contract,
            // table admin and contract owner private key is required.
            var txSigned = TransactionCreator.CreateTransactionBuilder(
                new [] { entry },
                new []
                {
                    new SignatureCredential(Utils.GetTableAdminKeyPair().PublicKey),
                    new SignatureCredential(Utils.GetOwnerKeyPair().PublicKey)
                })
                .Sign(Utils.GetTableAdminKeyPair().PrivateKey)
                .Sign(Utils.GetOwnerKeyPair().PrivateKey)
                .Build();

            await generalApi.SendTransactionAsync(txSigned);

            var result = await Utils.WaitTx(generalApi, txSigned.Id);
            Console.WriteLine($"txid={txSigned.Id}, result={result}");
        }

        //participantlist
        //:03c898153e55c32627422466a83ed40b9233c1583023dafa179a4f2a4804306574
        //:027774dc46331602d9cc57da74bfce060d636238f9a0d06f5818ac44800c584538
        //:0390fe3ec4769770ee89da70c72a6ebb7449e6e16cfdf973d9350bb9dd587773f1  //beneficiary
        //:02c31e96cfb1497e3312c28669bbb25bf32a9c28f1cd64a697bbc17ab57ed9e434  //beneficiary (2nd)
        //:03bdfe20157b5aeab5a6c47bf5abe887147fd7fff3ae7d9cd54186c8822711bf4c
        //:03f9e61ae23c85a6eb6b9260591d1793a4d0c2f0970b2d93fbc4434044a9880a4d
        private static async Task InvokeContract(IClient client)
        {
            string argument = "0338f9e2e7ad512fe039adaa509f7b555fb88719144945bd94f58887d8d54fed78";
            var generalApi = new GeneralApi(client);
            
            // Create gen entry
            var entry = new ContractInvoke(s_AssemblyId, ContractName, InstanceName, "vote", new[] { argument});

            // Create signed transaction with builder. To invoke a smart contract,
            // contract owner's private key is required.
            var txSigned = TransactionCreator.CreateTransactionBuilder(
                new [] { entry },
                new []
                {
                    new SignatureCredential(Utils.GetContractuser5KeyPair().PublicKey)
                })
                .Sign(Utils.GetContractuser5KeyPair().PrivateKey)
                .Build();

            await generalApi.SendTransactionAsync(txSigned);

            var result = await Utils.WaitTx(generalApi, txSigned.Id);
            Console.WriteLine($"txid={txSigned.Id}, result={result}");
        }

        private static async Task QueryMethod(IClient client, string method, string[] arguments)
        {
            // ContractClient has access to asset endpoints
            var contractClient = new ContractClient(client);

            var result = await contractClient.QueryContractAsync(s_AssemblyId, ContractName, InstanceName, method, arguments);
            Console.WriteLine($"value={result.Value}");
        }
    }
}
