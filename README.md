# dotnet-yieldly-sdk
[![Dev CI Status](https://dev.azure.com/gbo-devops/github-pipelines/_apis/build/status/Yieldly/Yieldly%20Dev%20CI?branchName=develop)](https://dev.azure.com/gbo-devops/github-pipelines/_build/latest?definitionId=4&branchName=develop)
[![Donate Algo](https://img.shields.io/badge/Donate-ALGO-000000.svg?style=flat)](https://algoexplorer.io/address/EJMR773OGLFAJY5L2BCZKNA5PXLDJOWJK4ED4XDYTYH57CG3JMGQGI25DQ)

Yieldly .NET SDK

# Overview
This library provides access to the [Yieldly](https://app.yieldly.finance/) No Loss Lottery and Staking contracts on the Algorand blockchain.

# Usage
This section contains examples for interacting with the lottery and staking contracts. It's possible to use this SDK without passing the Account object to SDK methods, see the `Verbose` example projects in the [/examples](/examples) directory.

Note, deposits and withdraws are to/from an escrow account, not the contracts themselves. 

## Lottery Deposit
Deposit ALGO in the no loss lottery.

```C#
// Initialize the client
var algodApi = new Algorand.V2.AlgodApi(
	Constant.AlgodMainnetHost, String.Empty);
var client = new YieldlyClient(algodApi);

// Deposit 10 ALGO in the no loss lottery
var amountToDeposit = Utils.AlgosToMicroalgos(10.0);

var result = client.LotteryDeposit(account, amountToDeposit);
```

## Lottery Withdrawal
Withdraw ALGO participating in the no loss lottery.

```C#
// Initialize the client
var algodApi = new Algorand.V2.AlgodApi(
	Constant.AlgodMainnetHost, String.Empty);
var client = new YieldlyClient(algodApi);

// Fetch all Yieldly amounts
var amounts = client.FetchAmounts(account.Address);

// Withdraw all ALGO currently deposited in the no loss lottery
var result = client.LotteryWithdraw(account, amounts.AlgoInLottery);
```

## Lottery Reward Claim
Claim reward from lottery participation. Note, this does not include winning the lottery, just the rewards in YLDY.

```C#
// Initialize the client
var algodApi = new Algorand.V2.AlgodApi(
	Constant.AlgodMainnetHost, String.Empty);
var client = new YieldlyClient(algodApi);

// Fetch all Yieldly amounts
var amounts = client.FetchAmounts(account.Address);

// Claim current Yieldy rewards from lottery
var result = client.LotteryClaimReward(account, amounts.LotteryReward.Yieldly);
```

## Staking Deposit
Deposit YLDY in the staking pool.

```C#
// Initialize the client
var algodApi = new Algorand.V2.AlgodApi(
	Constant.AlgodMainnetHost, String.Empty);
var client = new YieldlyClient(algodApi);

// Deposit 1000 YLDY in the Yieldly staking pool
var amountToDeposit = YieldlyUtils.YieldlyToMicroyieldly(1000.0);

var result = client.YieldlyStakingDeposit(account, amountToDeposit);
```

## Staking Withdrawal
Withdraw YLDY in the staking pool.

```C#
// Initialize the client
var algodApi = new Algorand.V2.AlgodApi(
	Constant.AlgodMainnetHost, String.Empty);
var client = new YieldlyClient(algodApi);

// Fetch all Yieldly amounts
var amounts = client.FetchAmounts(account.Address);

// Withdraw all YLDY currently deposited in the Yieldly staking pool
var result = client.YieldlyStakingWithdraw(account, amounts.YieldlyStaked);
```

## Staking Reward Claim
Claim rewards from staking pool participation.

```C#
// Initialize the client
var algodApi = new Algorand.V2.AlgodApi(
	Constant.AlgodMainnetHost, String.Empty);
var client = new YieldlyClient(algodApi);

// Fetch all Yieldly amounts
var amounts = client.FetchAmounts(account.Address);

// Withdraw all ALGO and YLDY currently available as rewards from Yieldly staking pool participation
var result = client.YieldyStakingClaimReward(account, amounts.StakingReward);
```

# Examples
Full examples, simple and verbose, can be found in [/example](/example).

# How?
This SDK was built by analyzing the transactions created by the [Yieldly](https://app.yieldly.finance/) website in [AlgoExporer](https://algoexplorer.io/). A special thanks [@JoshLmao](https://github.com/JoshLmao), his code provided a starting point for reward calculations. 

## Notes
The order of transactions in each transaction group is significant. Each transaction group, except lottery winning, has been tested.

## Special Thanks
Special thanks to [@JoshLmao](https://github.com/JoshLmao) for [yly-calc](https://github.com/JoshLmao/ydly-calc/blob/main/src/js/YLDYCalculation.js).

# Build
dotnet-yieldly-sdk build pipelines use the [Assembly Info Task](https://github.com/BMuuN/vsts-assemblyinfo-task) extension.

# License
dotnet-yieldly-sdk is licensed under a MIT license except for the exceptions listed below. See the LICENSE file for details.

## Exceptions
None.

# Disclaimer
Nothing in the repo constitutes professional and/or financial advice. Use this SDK at your own risk.