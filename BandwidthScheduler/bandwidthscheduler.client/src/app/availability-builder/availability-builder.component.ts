import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { BackendConnectService } from '../services/backend-connect.service';
import { AvailabilityEntry } from '../models/db/AvailabilityEntry';
import { CommitmentEntry } from '../models/db/CommitmentEntry';
import { StandardSnackbarService } from '../services/standard-snackbar.service';
import { HttpErrorResponse } from '@angular/common/http';
import { DateTimeRangeSelectorModel } from '../commonControls/date-time-range-selector/date-time-range-selector-model';

@Component({
  selector: 'app-availability-builder',
  templateUrl: './availability-builder.component.html',
  styleUrl: './availability-builder.component.scss',
})
export class AvailabilityBuilderComponent {
  private timeSpan = 30;

  public loading: boolean = false;

  public TimeRange: DateTimeRangeSelectorModel | undefined;

  public SelectedTimeRange(range: DateTimeRangeSelectorModel): void {
    console.log(range);
    this.TimeRange = range;
    this.currentAvailableModels = [];
    this.loading = true;
    this.backEnd.Availability.GetAllTimes(this.TimeRange).subscribe({
      complete: () => (this.loading = false),
      next: (value) => {
        console.log(value.commitments);
        this.currentAvailabilities = value.availabilities.map(
          (e) => new AvailabilityEntry(e)
        );

        this.currentCommitmentModels = value.commitments.map(
          (e) => new CommitmentEntry(e)
        );

        // fill out time blocks
        for (
          let i = 0;
          range.end > this.GetDateTransformed(range.start, i);
          i++
        ) {
          const startTime = this.GetDateTransformed(range.start, i);
          const endTime = this.GetDateTransformed(range.start, i + 1);
          this.currentAvailableModels.push({
            startTime: startTime,
            endTime: endTime,
            isSelected: this.currentAvailabilities.some(
              (e) =>
                // any intersection
                !(e.endTime <= startTime || e.startTime >= endTime)
            ),
            isDisabled: this.currentCommitmentModels.some(
              (e) =>
                // any intersection
                !(e.endTime <= startTime || e.startTime >= endTime)
            ),
          });
        }
      },
      error: (errorResp: HttpErrorResponse) => {
        this.loading = false;
        this.snackBar.OpenErrorMessage(errorResp.error);
      },
    });
  }

  currentAvailabilities: AvailabilityEntry[] = [];
  currentCommitmentModels: CommitmentEntry[] = [];
  currentAvailableModels: IAvailabilityEntryModel[] = [];

  constructor(
    private backEnd: BackendConnectService,
    private snackBar: StandardSnackbarService,
    private ref: ChangeDetectorRef
  ) {}

  public GetDateTransformed(date: Date, increment: number): Date {
    const cloneDate = new Date(date);

    cloneDate.setMinutes(this.timeSpan * increment);

    return cloneDate;
  }

  public OnSubmit(): void {
    this.backEnd.Availability.PutAllTimes(
      this.TimeRange!,
      this.currentAvailableModels
        .filter((e) => e.isSelected)
        .map<AvailabilityEntry>((e) => {
          const proto = AvailabilityEntry.Default();

          proto.startTime = e.startTime;
          proto.endTime = e.endTime;

          return proto;
        })
    ).subscribe({
      complete: () => (this.currentAvailableModels = []),
      error: () => {},
    });
  }
}

export interface IAvailabilityEntryModel {
  startTime: Date;
  endTime: Date;
  isSelected: boolean;
  isDisabled: boolean;
}
