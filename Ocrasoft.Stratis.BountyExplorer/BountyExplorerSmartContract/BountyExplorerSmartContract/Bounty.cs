using Stratis.SmartContracts;

public class Bounty : SmartContract
{
	public enum BountyState : int
	{
		New = 0,
		HasEnrollments = 1,
		IsAssigned = 2,
		UnderReview = 3,
		Finished = 4
	}


	public struct Enrollment
	{
		public Address Address { get; set; }
		public string Name { get; set; }
		public string Motivation { get; set; }
	}

	public struct BountyDeposit
	{
		public string Name { get; set; }
		public Address Address { get; set; }
		public ulong Amount { get; set; }
	}
	
	public Bounty(ISmartContractState smartContractState
			, string name
			, string description)
			: base(smartContractState)
	{
		Owner = Message.Sender;
		Name = name;
		Description = description;
		DeposittedBounties.Add(new BountyDeposit
		{
			Name = "owner",
			Address = Message.Sender,
			Amount = Message.Value
		});
		State = (int)BountyState.New;
	}

	public void DepositBounty(string funder)
	{
		Assert(State < (int)BountyState.HasEnrollments);
		DeposittedBounties.Add(new BountyDeposit
		{
			Name = funder,
			Address = Message.Sender,
			Amount = Message.Value
		});
	}

	public void Enroll(string name, string motivation)
	{
		Assert(State < (int)BountyState.IsAssigned);
		Enrollments.Add(new Enrollment
		{
			Address = Message.Sender,
			Name = name,
			Motivation = motivation
		});

		State = (int)BountyState.HasEnrollments;
	}

	public void PickEnrollment(Address enrollmentToPick)
	{
		Assert(State == (int)BountyState.HasEnrollments);
		Assert(Owner == Message.Sender);

		for(uint i = 0; i < Enrollments.Count; i++)
		{
			if (Enrollments[i].Address == enrollmentToPick)
			{
				Developer = Enrollments[i].Address;
				break;
			}
		}

		State = (int)BountyState.IsAssigned;
	}

	public void RequestReview()
	{
		Assert(State == (int)BountyState.IsAssigned);
		Assert(Developer == Message.Sender);

		State = (int)BountyState.UnderReview;
	}

	public void AcceptAndPayout()
	{
		Assert(State == (int)BountyState.UnderReview);
		Assert(Owner == Message.Sender);

		for (uint i = 0; i < DeposittedBounties.Count; i++)		
		{
			var item = DeposittedBounties[i];
			ITransferResult transferResult = TransferFunds(Developer, item.Amount);
			if (!transferResult.Success)
				return;
		}

		State = (int)BountyState.Finished;
	}

	public string Name
	{
		get => PersistentState.GetString(nameof(Name));
		set => PersistentState.SetString(nameof(Name), value);
	}

	public string Description
	{
		get => PersistentState.GetString(nameof(Description));
		set => PersistentState.SetString(nameof(Description), value);
	}

	public ulong TotalBounty
	{
		get
		{
			ulong result = 0;
			for (uint i = 0; i < DeposittedBounties.Count; i++)
			{
				result += DeposittedBounties[i].Amount;
			}
			return result;
		}
	}

	public Address Owner
	{
		get => PersistentState.GetAddress(nameof(Owner));
		set => PersistentState.SetAddress(nameof(Owner), value);
	}
	public Address Developer
	{
		get => PersistentState.GetAddress(nameof(Developer));
		set => PersistentState.SetAddress(nameof(Developer), value);
	}
	public int State
	{
		get => PersistentState.GetInt32(nameof(State));
		set => PersistentState.SetInt32(nameof(State), value);
	}
	public ISmartContractList<BountyDeposit> DeposittedBounties
		=> PersistentState.GetStructList<BountyDeposit>(nameof(DeposittedBounties));
	public ISmartContractList<Enrollment> Enrollments
		=> PersistentState.GetStructList<Enrollment>(nameof(Enrollments));
}