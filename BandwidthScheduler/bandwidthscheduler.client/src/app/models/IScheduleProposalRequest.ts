import { IScheduleProposalAmount } from './IScheduleProposalAmount';
import { ITeam } from './db/ITeam';

export interface IScheduleProposalRequest {
  selectedTeam: ITeam;
  proposal: IScheduleProposalAmount[];
}
