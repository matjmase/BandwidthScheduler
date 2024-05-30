import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TimeFrameModel, TriStateButton } from '../TimeFrameModel';
import { IScheduleProposalRequest } from '../../../models/IScheduleProposalRequest';
import { IScheduleProposalAmount } from '../../../models/IScheduleProposalAmount';
import { GridRenderingModel } from './grid-rendering-model';
import { CommitmentEntry } from '../../../models/db/CommitmentEntry';
import { IColorModel } from '../ColoredTimeFrameModel';
import { UserLegendModel } from '../../../commonControls/user-legend/user-legend-model';

@Component({
  selector: 'app-grid-rendering-proposal',
  templateUrl: './grid-rendering-proposal.component.html',
  styleUrl: './grid-rendering-proposal.component.scss',
})
export class GridRenderingProposalComponent {
  @Input() set Model(model: GridRenderingModel) {
    this.RenderingModel = model;

    this.RenderingModel.TimeFrames.forEach((tf) =>
      tf.Taken.forEach((t) => {
        if (this.ColorDict[t.user.email] === undefined) {
          this.ColorDict[t.user.email] = this.GenerateRandomColor();
        }
      })
    );

    this.LegendModel = [];
    for (let [k, v] of Object.entries(this.ColorDict)) {
      this.LegendModel.push({
        Name: k,
        Color: v,
      });
    }
  }

  public ColorDict: { [key: string]: IColorModel } = {};
  public LegendModel: UserLegendModel[] = [];

  @Output() Proposal: EventEmitter<IScheduleProposalAmount[]> =
    new EventEmitter<IScheduleProposalAmount[]>();

  public RenderingModel: GridRenderingModel | undefined;

  public ClearAllHover(): void {
    this.RenderingModel!.TimeFrames.forEach((e) => e.Open.SetLevelLeave());
  }

  public ClearSelection(): void {
    for (let timeFrame of this.RenderingModel!.TimeFrames) {
      for (let i = 0; i < timeFrame.Open.Level.length; i++) {
        timeFrame.Open.Level[i] = TriStateButton.NotSelected;
      }
    }
  }

  public GenerateProposal(): void {
    const proposal: IScheduleProposalAmount[] = [];

    for (let timeFrame of this.RenderingModel!.TimeFrames) {
      let finalLevel = 0;

      for (var i = timeFrame.Open.Level.length - 1; i >= 0; i--) {
        if (timeFrame.Open.Level[i] === TriStateButton.Selected) {
          finalLevel = i + 1;
          break;
        }
      }

      proposal.push({
        startTime: timeFrame.Open.StartTime,
        endTime: timeFrame.Open.EndTime,
        employees: finalLevel,
      });
    }

    this.Proposal.emit(proposal);
  }

  private GenerateRandomColor(): IColorModel {
    return {
      R: Math.random() * 255,
      G: Math.random() * 255,
      B: Math.random() * 255,
    };
  }
}
