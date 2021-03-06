﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.Enums;
using ReliableUdp.Logging;
using ReliableUdp.Packet;
using ReliableUdp.Utility;

using Utility;

namespace ReliableUdp.NetworkStatistic
{
	public class NetworkStatisticManagement
	{
		public FlowManagement FlowManagement { get; private set; }

		private int ping;
		private int rtt;
		private int avgRtt;
		private int rttCount;
		private int goodRttCount;

		public int TimeSinceLastPacket
		{
			get { return this.timeSinceLastPacket; }
		}

		public int Ping
		{
			get { return this.ping; }
		}

		public double ResendDelay
		{
			get { return this.avgRtt; }
		}

		private const int RTT_RESET_DELAY = 1000;
		private int rttResetTimer;

		private int timeSinceLastPacket;

		public NetworkStatisticManagement()
		{
			this.FlowManagement = new FlowManagement();

			// we start with an avgRtt because we don't want to have a resent delay of 0
			this.avgRtt = 27;
			this.rtt = 0;
		}

		public void ResetTimeSinceLastPacket()
		{
			this.timeSinceLastPacket = 0;
		}

		public void UpdateRoundTripTime(int roundTripTime)
		{
			//Calc average round trip time
			this.rtt += roundTripTime;
			this.rttCount++;
			this.avgRtt = this.rtt / this.rttCount;

			//flowmode 0 = fastest
			//flowmode max = lowest

			if (this.avgRtt < this.FlowManagement.GetStartRtt(this.FlowManagement.CurrentFlowMode - 1))
			{
				if (this.FlowManagement.CurrentFlowMode <= 0)
				{
					//Already maxed
					return;
				}

				this.goodRttCount++;
				if (this.goodRttCount > FlowManagement.FLOW_INCREASE_THRESHOLD)
				{
					this.goodRttCount = 0;
					this.FlowManagement.CurrentFlowMode--;

					Factory.Get<IUdpLogger>().Log($"Increased flow speed, RTT {this.avgRtt}, PPS {this.FlowManagement.GetPacketsPerSecond(this.FlowManagement.CurrentFlowMode)}");
				}
			}
			else if (this.avgRtt > this.FlowManagement.GetStartRtt(this.FlowManagement.CurrentFlowMode))
			{
				this.goodRttCount = 0;
				if (this.FlowManagement.CurrentFlowMode < this.FlowManagement.GetMaxFlowMode())
				{
					this.FlowManagement.CurrentFlowMode++;
					Factory.Get<IUdpLogger>().Log($"Decreased flow speed, RTT {this.avgRtt}, PPS {this.FlowManagement.GetPacketsPerSecond(this.FlowManagement.CurrentFlowMode)}");
				}
			}

			//recalc resend delay
			double avgRtt = this.avgRtt;
			if (avgRtt <= 0.0)
				avgRtt = 0.1;
		}

		public void Update(UdpPeer peer, int deltaTime, Action<UdpPeer, int> connectionLatencyUpdated)
		{
			this.FlowManagement.ResetFlowTimer(deltaTime);
			this.timeSinceLastPacket += deltaTime;

			//RTT - round trip time
			this.rttResetTimer += deltaTime;
			if (this.rttResetTimer >= RTT_RESET_DELAY)
			{
				this.rttResetTimer = 0;
				//Rtt update
				this.rtt = this.avgRtt;
				this.ping = this.avgRtt;
				connectionLatencyUpdated(peer, this.ping);
				this.rttCount = 1;
			}
		}
	}
}
