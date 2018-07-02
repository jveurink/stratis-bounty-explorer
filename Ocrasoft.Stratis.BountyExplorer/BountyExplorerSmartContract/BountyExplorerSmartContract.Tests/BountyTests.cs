using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stratis.SmartContracts;
using System.Collections.Generic;
using System.Linq;

namespace BountyExplorerSmartContract.Tests
{

	[TestClass]
	public class BountyTests
	{
		private static readonly Address CoinbaseAddress = (Address)"mxKorCkWmtrPoekfWiMzERJPhaT13nnkMy";
		private static readonly Address ContractOwnerAddress = (Address)"muXxezY249vn18Ho67qLnybEwzwp4t5Cwj";
		private static readonly Address ContractAddress = (Address)"muQuwkjrhCC26mTRJW7BivGBNAdZt25M1E";

		private static readonly Address SecondDepositerAddress = (Address)"muQuwkjrhCC26mTRJW7BivGBNAdZt25M1F";
		private static readonly Address Developer1Address = (Address)"muQuwkjrhCC26mTRJW7BivGBNAdZt25M2A";
		private static readonly Address Developer2Address = (Address)"muQuwkjrhCC26mTRJW7BivGBNAdZt25M3B";

		private const ulong ContractDeployBlockNumber = 1;
		private const ulong Duration = 20u;
		private const ulong GasLimit = 10000;

		private Dictionary<Address, ulong> BlockchainBalances;

		private TestSmartContractState SmartContractState;

		[TestInitialize]
		public void Initialize()
		{
			// Runs before each test
			BlockchainBalances = new Dictionary<Address, ulong>();
			BlockchainBalances.Add(ContractAddress, 0);
			BlockchainBalances.Add(ContractOwnerAddress, 1000);
			BlockchainBalances.Add(SecondDepositerAddress, 1000);
			BlockchainBalances.Add(Developer1Address, 1000);
			BlockchainBalances.Add(Developer2Address, 1000);

			var block = new TestBlock
			{
				Coinbase = CoinbaseAddress,
				Number = ContractDeployBlockNumber
			};
			var message = new TestMessage
			{
				ContractAddress = ContractAddress,
				GasLimit = (Gas)GasLimit,
				Sender = ContractOwnerAddress,
				Value = 0u
			};
			var getContractBalance = new Func<ulong>(() => BlockchainBalances[ContractAddress]);
			var persistentState = new TestPersistentState();
			var internalTransactionExecutor = new TestInternalTransactionExecutor(BlockchainBalances, ContractAddress);
			var gasMeter = new TestGasMeter((Gas)GasLimit);
			var hashHelper = new TestInternalHashHelper();

			this.SmartContractState = new TestSmartContractState(
				block,
				message,
				persistentState,
				gasMeter,
				internalTransactionExecutor,
				getContractBalance,
				hashHelper
			);
		}

		[TestMethod]
		public void TestConstruction()
		{
			var auction = new Bounty(SmartContractState, "Test Bounty SmartContract", 
				"Test the bouny smartcontract and tools");

			Assert.AreEqual(ContractOwnerAddress, SmartContractState.PersistentState.GetAddress("Owner"));
			Assert.AreEqual(0, SmartContractState.PersistentState.GetInt32("State"));
			Assert.AreEqual(1, SmartContractState.PersistentState.GetStructList<Bounty.BountyDeposit>("DeposittedBounties").Count);
		}

		private void SetSender(Address address, ulong amount)
		{
			((TestMessage)SmartContractState.Message).Sender = address;
			((TestMessage)SmartContractState.Message).Value = amount;
		}

		[TestMethod]
		public void TestCompleteFlow()
		{
			((TestMessage)SmartContractState.Message).Value = 100;

			var bounty = new Bounty(SmartContractState, "Test Bounty SmartContract",
				"Test the bouny smartcontract and tools");

			Assert.AreEqual(ContractOwnerAddress, SmartContractState.PersistentState.GetAddress("Owner"));
			Assert.AreEqual((int)Bounty.BountyState.New, SmartContractState.PersistentState.GetInt32("State"));
			Assert.AreEqual(1U, SmartContractState.PersistentState.GetStructList<Bounty.BountyDeposit>("DeposittedBounties").Count);

			// Add an extra deposit to the bounty. So the total bounty amount will become 150 STRAT
			SetSender(SecondDepositerAddress, 50);
			bounty.DepositBounty("Ocrasoft");

			Assert.AreEqual(150u, bounty.TotalBounty);
			Assert.AreEqual(2u, SmartContractState.PersistentState.GetStructList<Bounty.BountyDeposit>("DeposittedBounties").Count);

			// Enroll the first developer
			SetSender(Developer1Address, 0);
			bounty.Enroll("Jeroen Veurink", "I like to make some money :)");
			Assert.AreEqual((int)Bounty.BountyState.HasEnrollments, SmartContractState.PersistentState.GetInt32("State"));
			Assert.AreEqual(1u, SmartContractState.PersistentState.GetStructList<Bounty.Enrollment>("Enrollments").Count);

			// Enroll the second developer
			SetSender(Developer2Address, 0);
			bounty.Enroll("Wacky Hacker", "Give Me m0n3ys!");
			Assert.AreEqual(2u, SmartContractState.PersistentState.GetStructList<Bounty.Enrollment>("Enrollments").Count);

			// Pick the first enrollment as winner
			var firstEnrollmentAddress = SmartContractState.PersistentState.GetStructList<Bounty.Enrollment>("Enrollments")[0].Address;
			SetSender(ContractOwnerAddress, 0);
			bounty.PickEnrollment(firstEnrollmentAddress);
			Assert.AreEqual((int)Bounty.BountyState.IsAssigned, SmartContractState.PersistentState.GetInt32("State"));

			SetSender(Developer1Address, 0);
			bounty.RequestReview();
			Assert.AreEqual((int)Bounty.BountyState.UnderReview, SmartContractState.PersistentState.GetInt32("State"));

			SetSender(ContractOwnerAddress, 0);
			bounty.AcceptAndPayout();
			Assert.AreEqual((int)Bounty.BountyState.Finished, SmartContractState.PersistentState.GetInt32("State"));
		}
	}
}
