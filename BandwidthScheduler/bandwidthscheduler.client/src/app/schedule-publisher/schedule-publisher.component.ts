import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { TimeFrameModel, TriStateButton } from './TimeFrameModel';
import { IScheduleProposalRequest } from '../models/IScheduleProposalRequest';
import { BackendConnectService } from '../services/backend-connect.service';
import { IColorModel } from './ColoredTimeFrameModel';
import { GridRenderingFormModel } from './grid-rendering-form/grid-rendering-form-model';
import { IGridRenderingGeneratedModel } from './grid-rendering-generated/grid-rendering-generated-model';
import { ITeam } from '../models/db/ITeam';
import { IScheduleProposalAmount } from '../models/IScheduleProposalAmount';
import { IScheduleProposalUserProcessed } from '../models/IScheduleProposalUser';
import { IScheduleProposalResponse } from '../models/IScheduleProposalResponse';
import { HttpErrorResponse } from '@angular/common/http';
import { StandardSnackbarService } from '../services/standard-snackbar.service';
import { DateTimeRangeSelectorModel } from '../commonControls/date-time-range-selector/date-time-range-selector-model';

@Component({
  selector: 'app-schedule-publisher',
  templateUrl: './schedule-publisher.component.html',
  styleUrl: './schedule-publisher.component.scss',
})
export class SchedulePublisherComponent {
  private timeSpan = 30;
  private totalMinutes = 24 * 60;
  private totalBlock = this.totalMinutes / this.timeSpan;

  private proposalRequest: IScheduleProposalRequest | undefined;
  private proposalResponse: IScheduleProposalResponse | undefined;

  public SelectedTeam: ITeam | undefined;

  public SelectedTimeRange: DateTimeRangeSelectorModel | undefined;

  public RenderModel: GridRenderingFormModel | undefined;

  public TimeFrames: TimeFrameModel[] | undefined;

  public GeneratedModel: IGridRenderingGeneratedModel | undefined;

  constructor(
    private backend: BackendConnectService,
    private snackBar: StandardSnackbarService
  ) {}

  public SubmitTeam(team: ITeam) {
    this.SelectedTeam = team;
  }

  public SubmitRangeModel(model: DateTimeRangeSelectorModel) {
    this.SelectedTimeRange = model;
  }

  public SubmitRenderModel(model: GridRenderingFormModel) {
    this.RenderModel = model;

    this.TimeFrames = [];

    const newTimeFrames: TimeFrameModel[] = [];

    console.log(this.SelectedTimeRange);

    for (
      let i = 0;
      this.GetDateTransformed(i + 1, this.SelectedTimeRange!.start) <=
      this.SelectedTimeRange!.end;
      i++
    ) {
      newTimeFrames.push(
        new TimeFrameModel(
          this.GetDateTransformed(i, this.SelectedTimeRange!.start),
          this.GetDateTransformed(i + 1, this.SelectedTimeRange!.start),
          new Array(this.RenderModel.maxEmployees).fill(false)
        )
      );
    }

    this.TimeFrames = newTimeFrames;
  }

  public GetDateTransformed(increment: number, currentDate: Date): Date {
    const cloneDate = new Date(currentDate);

    cloneDate.setMinutes(cloneDate.getMinutes() + this.timeSpan * increment);

    return cloneDate;
  }

  public SubmitProposal(proposal: IScheduleProposalAmount[]) {
    this.proposalRequest = {
      selectedTeam: this.SelectedTeam!,
      proposal: proposal,
    };

    this.backend.Publish.RequestScheduleTimes(this.proposalRequest).subscribe({
      next: (resp) => {
        this.proposalResponse = resp;
        this.GeneratedModel = {
          maxNumberOfPeople: this.RenderModel!.maxEmployees,
          proposal: this.proposalRequest!,
          responseRaw: this.proposalResponse!,
        };
      },
    });
  }

  public SubmitFinal() {
    this.backend.Publish.SubmitSchedule({
      ProposalRequest: this.proposalRequest!,
      ProposalResponse: this.proposalResponse!,
    }).subscribe({
      complete: () => window.location.reload(),
      error: (errorResp: HttpErrorResponse) =>
        this.snackBar.OpenErrorMessage(errorResp.error),
    });
  }
}

export class ProposalResponseWrapper {
  constructor(
    public response: IScheduleProposalUserProcessed,
    public colorModel: IColorModel
  ) {}
}
