using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents {

	public class WarControls : PRoConPluginAPI, IPRoConPluginInterface {
	
		//devsettings
		private bool isDebug = true;

		//time to react for the other team after the first ready
		private int readyCountdown = 30;

		//time until server will restart
		private int startCountdown = 10;
		
		
		//all users
		private string commandPrefix = "@";
		private string readyCommand = "ready";
		
		//internal vars
		private bool team1Ready = false;
		private bool team2Ready = false;
		private int team1ReadyTime = 0;
		private int team2ReadyTime = 0;
        private int team1PlayerCount = 0;
		private int team2PlayerCount = 0;
		
		//@todo: read up on multidimensional lists... 
		private List<string> team1Players = new List<string>();
		private List<string> team2Players = new List<string>();
		
		public void init() {
		
			this.Debug("WarControls enabled...");
		}

		public void destroy(){
			this.Debug("WarControls disabled...");
		}
		
		public override void OnGlobalChat(string speaker, string message) {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
			string[] arguments = message.Split(' ');
			if(arguments[0].CompareTo(this.commandPrefix+this.readyCommand) == 0 && this.getNumberOfArguments(message) == 1){
				this.setReady(speaker);
			}
		}
		
		public void setReady(string playerName){
			int teamID = this.getTeamByPlayer(playerName);
			this.Debug("playername :"+playerName);
			int otherTeamID = 0;
			
			if(teamID == 1){
				otherTeamID = 2;
			} else {
				otherTeamID = 1;
			}
			
			int readyTime = this.getReadyTime(teamID);
			int otherReadyTime = this.getReadyTime(otherTeamID);
			
			if(Math.Abs(readyTime-this.getUnixTimeStamp()) > this.readyCountdown){

				this.setReadyTime(teamID);
				this.ExecuteCommand("procon.protected.send", "admin.say", playerName+" has set team "+this.getTeamName(teamID)+" ready", "all");
				this.Debug("readyTime:"+readyTime);
				this.Debug("otherReadyTime:"+otherReadyTime);
				if((readyTime-otherReadyTime) > this.readyCountdown){
					this.team1ReadyTime = 0;
					this.team2ReadyTime = 0;
					this.ExecuteCommand("procon.protected.send", "admin.say", "one of the teams hasn't reacted fast enoguh, resetting both teams", "all");
				} else if(readyTime != otherReadyTime) {
					this.startMatch();
				}
			} else {
					this.ExecuteCommand("procon.protected.send", "admin.say", "Team "+this.getTeamName(teamID)+" is already ready", "all");
			}
		}

        public void resetReadyTime(){
            this.team1ReadyTime = 0;
            this.team2ReadyTime = 0;
        }

        public string getTeamName(int teamID){
            if (teamID == 1){
                return "Russian Army";
            } else {
                return "U.S. Army";
            }

        }
		
		public void startMatch(){
            this.resetReadyTime();
			for(int i = 0; i < this.startCountdown; i++){
				this.ExecuteCommand("procon.protected.send", "admin.yell", "Live after restart in "+(this.startCountdown - i), "all");
			}
			this.ExecuteCommand("procon.protected.send", "mapList.restartRound");
		}
		
		public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset) {

			List<string> tmpTeam1Players = new List<string>();
			List<string> tmpTeam2Players = new List<string>();
			
			foreach(CPlayerInfo player in players){
					if(player.TeamID == 1){
						tmpTeam1Players.Add(player.SoldierName);
					} else if(player.TeamID == 2){
						tmpTeam2Players.Add(player.SoldierName);
					}
			}
			this.team1Players = tmpTeam1Players;
			this.team2Players = tmpTeam2Players;
		}

		public int getTeamByPlayer(string playerName){
			this.Debug("playername: "+playerName);
			foreach(string player in team1Players){
				if(player == playerName){
					return 1;
				}
			}
			foreach(string player in team2Players){
				if(player == playerName){
					return 2;
				}
			}
			return 0;
		}
		
		public int getReadyTime(int teamID){
			if(teamID == 1){
				return this.team1ReadyTime;
			} else {
				return this.team2ReadyTime;
			}
		}
		
		public int getOtherTeamID(int teamID){
			if(teamID == 1){
				return 2;
			} else {
				return 1;
			}
			
		}
		
		public void setReadyTime(int teamID){
			if(teamID == 1){
				this.team1ReadyTime = this.getUnixTimeStamp();
			} else {
				this.team2ReadyTime = this.getUnixTimeStamp();
			}
		}
		
		public int getUnixTimeStamp(){
			TimeSpan _TimeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
			return unchecked((int)_TimeSpan.TotalSeconds);
		}
				
		
		public int getNumberOfArguments(string message){
			return message.Split(' ').Length;
		}
		
		public void Debug(string message){
			if(this.isDebug){
				this.LogWrite(message);
			}
		}

		public void LogWrite(String msg){
			this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
		}
		
        public string GetPluginName() {
            return "WarControls";
        }

        public string GetPluginVersion() {
            return "0.1a";
        }

        public string GetPluginAuthor() {
            return "redshark1802";
        }

        public string GetPluginWebsite() {
            return "github.com/redshark1802";
        }
		
        public string GetPluginDescription() {
            return @"<h2>WarControls</h2>";
		}
		
		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
			this.RegisterEvents(this.GetType().Name, "OnGlobalChat", "OnListPlayers"); 
		}
		
		public void OnPluginEnable() {
			this.init();
		}
		
		public void OnPluginDisable() {
			this.destroy();
		}
		
		public List<CPluginVariable> GetDisplayPluginVariables() {
			List<CPluginVariable> varDisplayList = new List<CPluginVariable>();
			
			return varDisplayList;
		}
		
		public List<CPluginVariable> GetPluginVariables() {
			List<CPluginVariable> varList = new List<CPluginVariable>();
			
			return varList;
		}
		public void SetPluginVariable(string strVariable, string strValue) {
		}
	}
}