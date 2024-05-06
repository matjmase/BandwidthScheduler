import { ICommitmentResponse } from './ICommitmentResponse';

export class CommitmentEntry {
  id: number;
  userId: number;
  userEmail: string;
  teamId: number;
  teamName: string;
  startTime: Date;
  endTime: Date;

  constructor(resp: ICommitmentResponse) {
    this.id = resp.id;
    this.userId = resp.userId;
    this.userEmail = resp.userEmail;
    this.teamId = resp.teamId;
    this.teamName = resp.teamName;
    this.startTime = new Date(Date.parse(resp.startTime));
    this.endTime = new Date(Date.parse(resp.endTime));
  }
}
