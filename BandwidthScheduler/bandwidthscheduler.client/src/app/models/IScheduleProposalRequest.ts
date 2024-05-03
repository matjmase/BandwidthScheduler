import { IScheduleProposal } from './IScheduleProposal';
import { ITeam } from './ITeam';

export interface IScheduleProposalRequest {
  selectedTeam: ITeam;
  proposal: IScheduleProposal[];
}
