export interface IScheduleProposalUser {
  userId: number;
  email: string;
  startTime: string;
  endTime: string;
}

export interface IScheduleProposalUserProcessed {
  userId: number;
  email: string;
  startTime: Date;
  endTime: Date;
}
