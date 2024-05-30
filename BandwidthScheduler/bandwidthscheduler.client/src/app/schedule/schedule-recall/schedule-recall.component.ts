import { Component } from '@angular/core';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';
import { ITeam } from '../../models/db/ITeam';
import { IGridLegendReadOnlyModel } from '../common/grid-legend-read-only/grid-legend-read-only-model';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';
import { BackendConnectService } from '../../services/backend-connect.service';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import {
  ColoredTimeFrameModel,
  IColorModel,
} from '../schedule-publisher/ColoredTimeFrameModel';
import { UserLegendModel } from '../../commonControls/user-legend/user-legend-model';

@Component({
  selector: 'app-schedule-recall',
  templateUrl: './schedule-recall.component.html',
  styleUrl: './schedule-recall.component.scss',
})
export class ScheduleRecallComponent {
  private timeSpan = 30;

  public SelectedTeam: ITeam | undefined;

  public SelectedTimeRange: DateTimeRangeSelectorModel | undefined;

  public GridModel: IGridLegendReadOnlyModel | undefined;

  private colorDict: { [key: string]: IColorModel } = {};

  constructor(
    private backend: BackendConnectService,
    private snackBar: StandardSnackbarService
  ) {}

  public SubmitTeam(team: ITeam): void {
    this.SelectedTeam = team;
    this.TryGetCommitments();
  }

  public SubmitRangeModel(model: DateTimeRangeSelectorModel): void {
    this.SelectedTimeRange = model;
    this.TryGetCommitments();
  }

  private TryGetCommitments(): void {
    if (this.SelectedTeam && this.SelectedTimeRange) {
      const range = this.SelectedTimeRange!;

      this.backend.Schedule.GetCommitments(
        this.SelectedTimeRange,
        this.SelectedTeam.id
      ).subscribe({
        next: (commitments) => {
          const commitEntries = commitments.map((c) => new CommitmentEntry(c));

          for (let entry of commitEntries) {
            if (this.colorDict[entry.user.email] === undefined) {
              this.colorDict[entry.user.email] = {
                R: Math.random() * 255,
                G: Math.random() * 255,
                B: Math.random() * 255,
              };
            }
          }

          const timeFrames: ColoredTimeFrameModel[] = [];
          let maxHeight = 0;

          for (
            let i = 0;
            this.GetDateTransformed(i + 1, range.start) <= range.end;
            i++
          ) {
            const startTime = this.GetDateTransformed(i, range.start);
            const endTime = this.GetDateTransformed(i + 1, range.start);

            const commitmentsToAdd = commitEntries.filter(
              (c) => !(endTime <= c.startTime || startTime >= c.endTime)
            );

            maxHeight = Math.max(maxHeight, commitmentsToAdd.length);

            timeFrames.push({
              StartTime: startTime,
              EndTime: endTime,
              Color: [],
            });

            for (let item of commitmentsToAdd) {
              timeFrames[timeFrames.length - 1].Color.push(
                this.colorDict[item.user.email]
              );
            }
          }

          for (let i = 0; i < timeFrames.length; i++) {
            const bufferToAdd = maxHeight - timeFrames[i].Color.length;

            for (let j = 0; j < bufferToAdd; j++) {
              timeFrames[i].Color.push({
                R: 255,
                G: 255,
                B: 255,
              });
            }
          }

          const legendModel: UserLegendModel[] = [];

          for (let [k, v] of Object.entries(this.colorDict)) {
            legendModel.push({
              Name: k,
              Color: v,
            });
          }

          this.GridModel = {
            LegendModel: legendModel,
            ColoredFrames: timeFrames,
          };
        },
      });
    }
  }

  public GetDateTransformed(increment: number, currentDate: Date): Date {
    const cloneDate = new Date(currentDate);

    cloneDate.setMinutes(cloneDate.getMinutes() + this.timeSpan * increment);

    return cloneDate;
  }
}
