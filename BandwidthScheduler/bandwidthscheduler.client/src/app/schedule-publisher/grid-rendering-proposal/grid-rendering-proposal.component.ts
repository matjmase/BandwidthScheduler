import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TimeFrameModel, TriStateButton } from '../TimeFrameModel';
import { IScheduleProposalRequest } from '../../models/IScheduleProposalRequest';
import { IScheduleProposalAmount } from '../../models/IScheduleProposalAmount';

@Component({
  selector: 'app-grid-rendering-proposal',
  templateUrl: './grid-rendering-proposal.component.html',
  styleUrl: './grid-rendering-proposal.component.scss',
})
export class GridRenderingProposalComponent {
  @Input() set Model(model: TimeFrameModel[]) {
    this.TimeFrames = model;
  }

  @Output() Proposal: EventEmitter<IScheduleProposalAmount[]> =
    new EventEmitter<IScheduleProposalAmount[]>();

  public TimeFrames: TimeFrameModel[] = [];

  public ClearAllHover(): void {
    this.TimeFrames.forEach((e) => e.SetLevelLeave());
  }

  public ClearSelection(): void {
    for (let timeFrame of this.TimeFrames) {
      for (let i = 0; i < timeFrame.Level.length; i++) {
        timeFrame.Level[i] = TriStateButton.NotSelected;
      }
    }
  }

  public GenerateProposal(): void {
    const proposal: IScheduleProposalAmount[] = [];

    for (let timeFrame of this.TimeFrames) {
      let finalLevel = 0;

      for (var i = timeFrame.Level.length - 1; i >= 0; i--) {
        if (timeFrame.Level[i] === TriStateButton.Selected) {
          finalLevel = i + 1;
          break;
        }
      }

      proposal.push({
        startTime: timeFrame.StartTime,
        endTime: timeFrame.EndTime,
        employees: finalLevel,
      });
    }

    this.Proposal.emit(proposal);
  }
}
