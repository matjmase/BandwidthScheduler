import { HttpErrorResponse } from '@angular/common/http';
import {
  AfterViewChecked,
  AfterViewInit,
  Component,
  OnInit,
} from '@angular/core';
import { AvailabilityEntry } from '../../models/db/AvailabilityEntry';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';
import { BackendConnectService } from '../../services/backend-connect.service';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import { IAvailabilityEntryModel } from './IAvailabilityEntryModel';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { NotificationType } from '../../nav-bar/INotificationWrapper';
import { AvailabilityNotificationEntry } from '../../models/db/AvailabilityNotificationEntry';
import { IAvailabilityNotification } from '../../models/db/IAvailabilityNotification';
import { IDateRangeSelectorModel } from '../../commonControls/date-range-selector/IDateRangeSelectorModel';

@Component({
  selector: 'app-availability',
  templateUrl: './availability.component.html',
  styleUrl: './availability.component.scss',
})
export class AvailabilityComponent implements OnInit {
  private timeSpan = 30;

  public loading: boolean = false;

  public TimeRange: IDateRangeSelectorModel | undefined;

  currentAvailabilities: AvailabilityEntry[] = [];
  currentCommitmentModels: CommitmentEntry[] = [];
  currentAvailableModels: IAvailabilityEntryModel[] = [];

  constructor(
    private backEnd: BackendConnectService,
    private snackBar: StandardSnackbarService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.CheckAndGetAvailabilityNotification();

    this.router.events.subscribe((ev) => {
      if (ev instanceof NavigationEnd) {
        this.CheckAndGetAvailabilityNotification();
      }
    });
  }

  public TimeRangeModelSubmit(newTime: IDateRangeSelectorModel): void {
    this.SelectedTimeRange(newTime);
  }

  private CheckAndGetAvailabilityNotification(): void {
    const notiType = this.route.snapshot.paramMap.get('notificationType');
    const notification = this.route.snapshot.paramMap.get('notification');

    if (
      notiType &&
      notification &&
      <NotificationType>JSON.parse(notiType) === NotificationType.Availability
    ) {
      const availability = <IAvailabilityNotification>JSON.parse(notification);

      try {
        const availEntry = new AvailabilityNotificationEntry(availability);

        const newTimeModel: IDateRangeSelectorModel = {
          start: availEntry.availability.startTime,
          end: availEntry.availability.endTime,
        };

        this.TimeRange = newTimeModel;

        this.SelectedTimeRange(this.TimeRange);
      } catch (err) {
        console.log('Error parsing availability entry from url');
      }
    }
  }

  public GetDateTransformed(date: Date, increment: number): Date {
    const cloneDate = new Date(date);

    cloneDate.setMinutes(cloneDate.getMinutes() + this.timeSpan * increment);

    return cloneDate;
  }

  public SelectedTimeRange(range: IDateRangeSelectorModel): void {
    this.currentAvailableModels = [];
    this.loading = true;

    this.backEnd.Availability.GetAllTimes(range).subscribe({
      complete: () => (this.loading = false),
      next: (value) => {
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
                !(e.endTime <= startTime && e.startTime >= endTime)
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
