import { ICommitment } from './ICommitment';
import { ITeam } from './ITeam';
import { IUser } from './IUser';

export class CommitmentEntry {
  id: number;
  userId: number;
  teamId: number;
  startTime: Date;
  endTime: Date;
  team: ITeam;
  user: IUser;

  constructor(resp: ICommitment) {
    this.id = resp.id;
    this.userId = resp.userId;
    this.teamId = resp.teamId;
    this.startTime = new Date(Date.parse(resp.startTime));
    this.endTime = new Date(Date.parse(resp.endTime));
    this.team = resp.team;
    this.user = resp.user;
  }
}
