export interface IScheduleProposalResponse {
  userId: number;
  email: string;
  startTime: string;
  endTime: string;
}

export interface IScheduleProposalResponseProcessed {
  userId: number;
  email: string;
  startTime: Date;
  endTime: Date;
}
