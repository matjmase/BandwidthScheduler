import { CommitmentEntry } from '../../models/db/CommitmentEntry';
import { TimeFrameModel } from '../TimeFrameModel';

export interface GridRenderingModel {
  TimeFrames: GridRenderingTimeFrame[];
}

export interface GridRenderingTimeFrame {
  StartTime: Date;
  EndTime: Date;

  Open: TimeFrameModel;
  Taken: CommitmentEntry[];
}
