import { ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { TimeFrameModel, TriStateButton } from './TimeFrameModel';
import { IScheduleProposalRequest } from '../../models/IScheduleProposalRequest';
import { BackendConnectService } from '../../services/backend-connect.service';
import { IColorModel } from './ColoredTimeFrameModel';
import { IGridRenderingGeneratedModel } from './grid-rendering-generated/grid-rendering-generated-model';
import { ITeam } from '../../models/db/ITeam';
import { IScheduleProposalAmount } from '../../models/IScheduleProposalAmount';
import { IScheduleProposalUserProcessed } from '../../models/IScheduleProposalUser';
import { IScheduleProposalResponse } from '../../models/IScheduleProposalResponse';
import { HttpErrorResponse } from '@angular/common/http';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';
import {
  GridRenderingModel,
  GridRenderingTimeFrame,
} from './grid-rendering-proposal/grid-rendering-model';
import { ICommitment } from '../../models/db/ICommitment';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';
import { MessageModalBoxComponent } from '../../commonControls/message-modal-box/message-modal-box.component';
import {
  IMessageModalBoxModel,
  MessageModalBoxType,
} from '../../commonControls/message-modal-box/message-modal-box-model';

@Component({
  selector: 'app-schedule-publisher',
  templateUrl: './schedule-publisher.component.html',
  styleUrl: './schedule-publisher.component.scss',
})
export class SchedulePublisherComponent {
  @ViewChild('submitModal') submitModal!: MessageModalBoxComponent;
  public SubmitModalModel: IMessageModalBoxModel = {
    title: 'Confirmation',
    description:
      'Are you sure you want to recall all commitments during this time period?',
    type: MessageModalBoxType.Confirmation,
  };

  private timeSpan = 30;

  private proposalRequest: IScheduleProposalRequest | undefined;
  private proposalResponse: IScheduleProposalResponse | undefined;

  public SelectedTeam: ITeam | undefined;

  public SelectedTimeRange: DateTimeRangeSelectorModel | undefined;

  public SelectedMaxEmployees: number | undefined;

  private commitmentEntries: CommitmentEntry[] = [];
  private actualMaxEmployees: number = 0;

  public RenderingModel: GridRenderingModel | undefined;

  public GeneratedModel: IGridRenderingGeneratedModel | undefined;

  constructor(
    private backend: BackendConnectService,
    private snackBar: StandardSnackbarService
  ) {}

  public SubmitTeam(team: ITeam) {
    this.SelectedTeam = team;
    this.TryCreateRenderingModel();
  }

  public SubmitRangeModel(model: DateTimeRangeSelectorModel) {
    this.SelectedTimeRange = model;
    this.TryCreateRenderingModel();
  }

  public SubmitRenderModel(max: number) {
    this.SelectedMaxEmployees = max;
    this.TryCreateRenderingModel();
  }

  private TryCreateRenderingModel() {
    if (
      this.SelectedMaxEmployees &&
      this.SelectedTeam &&
      this.SelectedTimeRange
    ) {
      this.backend.Schedule.GetCommitments(
        this.SelectedTimeRange,
        this.SelectedTeam.id
      ).subscribe({
        next: (commitments) => {
          this.commitmentEntries = commitments.map(
            (c) => new CommitmentEntry(c)
          );
          this.CreateRenderingModel(
            this.SelectedMaxEmployees!,
            this.SelectedTimeRange!,
            this.commitmentEntries
          );
        },
      });
    }
  }

  private CreateRenderingModel(
    max: number,
    range: DateTimeRangeSelectorModel,
    commitments: CommitmentEntry[]
  ) {
    const blockedOutCommitments: CommitmentEntry[][] = [];

    let minCommitment = 0;
    for (
      let i = 0;
      this.GetDateTransformed(i + 1, range.start) <= range.end;
      i++
    ) {
      const startTime = this.GetDateTransformed(i, range.start);
      const endTime = this.GetDateTransformed(i + 1, range.start);

      const commitmentsToAdd = commitments.filter(
        (c) => !(endTime <= c.startTime || startTime >= c.endTime)
      );

      minCommitment = Math.max(minCommitment, commitmentsToAdd.length);

      blockedOutCommitments.push(commitmentsToAdd);
    }

    const newTimeFrames: TimeFrameModel[] = [];
    this.actualMaxEmployees = Math.max(minCommitment, max);

    for (
      let i = 0;
      this.GetDateTransformed(i + 1, range.start) <= range.end;
      i++
    ) {
      const startTime = this.GetDateTransformed(i, range.start);
      const endTime = this.GetDateTransformed(i + 1, range.start);

      const blockedOut = blockedOutCommitments[i].length;

      newTimeFrames.push(
        new TimeFrameModel(
          startTime,
          endTime,
          new Array(this.actualMaxEmployees - blockedOut).fill(
            TriStateButton.NotSelected
          ) as TriStateButton[]
        )
      );
    }

    const timeFrames: GridRenderingTimeFrame[] = [];

    for (
      let i = 0;
      this.GetDateTransformed(i + 1, range.start) <= range.end;
      i++
    ) {
      const startTime = this.GetDateTransformed(i, range.start);
      const endTime = this.GetDateTransformed(i + 1, range.start);

      const timeFrame: GridRenderingTimeFrame = {
        StartTime: startTime,
        EndTime: endTime,
        Open: newTimeFrames[i],
        Taken: blockedOutCommitments[i],
      };

      timeFrames.push(timeFrame);
    }

    this.RenderingModel = {
      TimeFrames: timeFrames,
    };
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

    this.backend.SchedulePublish.RequestScheduleTimes(
      this.proposalRequest
    ).subscribe({
      next: (resp) => {
        this.proposalResponse = resp;
        this.GeneratedModel = {
          maxNumberOfPeople: this.actualMaxEmployees,
          existingCommitments: this.commitmentEntries,
          proposal: this.proposalRequest!,
          responseRaw: this.proposalResponse!,
        };
      },
    });
  }

  public async SubmitFinal() {
    const confirm = await this.submitModal.ShowModal();

    if (!confirm) return;

    this.backend.SchedulePublish.SubmitSchedule({
      ProposalRequest: this.proposalRequest!,
      ProposalResponse: this.proposalResponse!,
    }).subscribe({
      complete: () => {
        this.SelectedTeam = undefined;
        this.SelectedTimeRange = undefined;
        this.SelectedMaxEmployees = undefined;
        this.RenderingModel = undefined;
        this.GeneratedModel = undefined;

        this.snackBar.OpenConfirmationMessage(
          'Successfully scheduled for the time period.'
        );
      },
      error: () =>
        this.snackBar.OpenErrorMessage('Error submitting the schdule.'),
    });
  }
}

export class ProposalResponseWrapper {
  constructor(
    public response: IScheduleProposalUserProcessed,
    public colorModel: IColorModel
  ) {}
}
