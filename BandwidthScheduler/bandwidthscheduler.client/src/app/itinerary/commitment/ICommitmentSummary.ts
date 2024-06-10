import { TimeSpan } from '../../models/TimeSpan';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';

export interface ICommitmentSummary {
  team: string;
  duration: TimeSpan;
  entries: CommitmentEntry[];
}
