import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { TimeFrameModel, TriStateButton } from './TimeFrameModel';
import { IScheduleProposalRequest } from '../models/IScheduleProposalRequest';
import { BackendConnectService } from '../services/backend-connect.service';
import { IColorModel } from './ColoredTimeFrameModel';
import { GridRenderingFormModel } from './grid-rendering-form/grid-rendering-form-model';
import { IGridRenderingGeneratedModel } from './grid-rendering-generated/grid-rendering-generated-model';
import { ITeam } from '../models/ITeam';
import { IScheduleProposalAmount } from '../models/IScheduleProposalAmount';
import { IScheduleProposalUserProcessed } from '../models/IScheduleProposalUser';

@Component({
  selector: 'app-schedule-publisher',
  templateUrl: './schedule-publisher.component.html',
  styleUrl: './schedule-publisher.component.scss',
})
export class SchedulePublisherComponent {
  private timeSpan = 30;
  private totalMinutes = 24 * 60;
  private totalBlock = this.totalMinutes / this.timeSpan;

  public SelectedTeam: ITeam | undefined;

  public RenderModel: GridRenderingFormModel | undefined;

  public TimeFrames: TimeFrameModel[] | undefined;

  public GeneratedModel: IGridRenderingGeneratedModel | undefined;

  constructor(private backend: BackendConnectService) {}

  public SubmitTeam(team: ITeam) {
    this.SelectedTeam = team;
  }

  public SubmitRenderModel(model: GridRenderingFormModel) {
    this.RenderModel = model;

    this.TimeFrames = [];

    const newTimeFrames: TimeFrameModel[] = [];

    for (let i = 0; i < this.totalBlock; i++) {
      newTimeFrames.push(
        new TimeFrameModel(
          this.GetDateTransformed(i, this.RenderModel.currentDate),
          this.GetDateTransformed(i + 1, this.RenderModel.currentDate),
          new Array(this.RenderModel.maxEmployees).fill(false)
        )
      );
    }

    this.TimeFrames = newTimeFrames;
  }

  public GetDateTransformed(increment: number, currentDate: Date): Date {
    const cloneDate = new Date(currentDate);

    cloneDate.setMinutes(this.timeSpan * increment);

    return cloneDate;
  }

  public SubmitProposal(proposal: IScheduleProposalAmount[]) {
    const request: IScheduleProposalRequest = {
      selectedTeam: this.SelectedTeam!,
      proposal: proposal,
    };

    this.backend.Publish.RequestScheduleTimes(request).subscribe({
      next: (resp) => {
        this.GeneratedModel = {
          maxNumberOfPeople: this.RenderModel!.maxEmployees,
          proposal: request,
          responseRaw: resp,
        };
      },
    });
  }
}

export class ProposalResponseWrapper {
  constructor(
    public response: IScheduleProposalUserProcessed,
    public colorModel: IColorModel
  ) {}
}
