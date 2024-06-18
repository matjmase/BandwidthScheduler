import { Component, OnInit } from '@angular/core';
import { BackendConnectService } from '../../services/backend-connect.service';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';
import { ICommitmentSummary } from './ICommitmentSummary';
import { TimeSpan } from '../../models/TimeSpan';
import {
  animate,
  state,
  style,
  transition,
  trigger,
} from '@angular/animations';
import { IDateRangeSelectorModel } from '../../commonControls/date-range-selector/IDateRangeSelectorModel';
import { Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { NotificationType } from '../../nav-bar/INotificationWrapper';
import { ICommitmentNotification } from '../../models/db/ICommitmentNotification';
import { CommitmentNotificationEntry } from '../../models/db/CommitmentNotificationEntry';

@Component({
  selector: 'app-commitment',
  templateUrl: './commitment.component.html',
  styleUrl: './commitment.component.scss',
  animations: [
    trigger('detailExpand', [
      state('collapsed,void', style({ height: '0px', minHeight: '0' })),
      state('expanded', style({ height: '*' })),
      transition(
        'expanded <=> collapsed',
        animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')
      ),
    ]),
  ],
})
export class CommitmentComponent implements OnInit {
  public loading: boolean = false;

  public TimeRange: IDateRangeSelectorModel | undefined;

  public DisplayedColumns: string[] = ['team', 'duration'];
  public DisplayedColumnsWithExpand: string[] = [
    ...this.DisplayedColumns,
    'expand',
  ];
  public InnerDisplayColumns: string[] = [
    'startTime',
    'endTime',
    'differential',
  ];
  public Summaries: ICommitmentSummary[] = [];
  public ExpandedElement: ICommitmentSummary | null = null;

  public TotalDuration: TimeSpan | undefined;

  constructor(
    private backend: BackendConnectService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.CheckAndGetCommitmentNotification();

    this.router.events.subscribe((ev) => {
      if (ev instanceof NavigationEnd) {
        this.CheckAndGetCommitmentNotification();
      }
    });
  }

  private CheckAndGetCommitmentNotification(): void {
    const notiType = this.route.snapshot.paramMap.get('notificationType');
    const notification = this.route.snapshot.paramMap.get('notification');

    if (
      notiType &&
      notification &&
      <NotificationType>JSON.parse(notiType) === NotificationType.Commitment
    ) {
      const commitment = <ICommitmentNotification>JSON.parse(notification);

      try {
        const commitEntry = new CommitmentNotificationEntry(commitment);

        const newTimeModel: IDateRangeSelectorModel = {
          start: commitEntry.commitment.startTime,
          end: commitEntry.commitment.endTime,
        };

        this.TimeRange = newTimeModel;

        this.SelectedDateRange(this.TimeRange);
      } catch (err) {
        console.log('Error parsing availability entry from url');
      }
    }
  }

  public SelectedDateRange(range: IDateRangeSelectorModel): void {
    this.loading = true;
    this.backend.Commitment.GetUserCommitments(range).subscribe({
      next: (commitments) => {
        this.CreateSummaries(commitments, range.start, range.end);
      },
      complete: () => (this.loading = false),
      error: () => (this.loading = false),
    });
  }

  private CreateSummaries(
    entries: CommitmentEntry[],
    rStart: Date,
    rEnd: Date
  ) {
    const uniqueTeams: { [key: string]: CommitmentEntry[] } = {};

    for (let entry of entries) {
      const teamName = entry.team.name;
      if (teamName in uniqueTeams) {
        uniqueTeams[teamName].push(entry);
      } else {
        uniqueTeams[teamName] = [entry];
      }
    }

    this.Summaries = [];

    this.TotalDuration = new TimeSpan(0);
    for (let [k, v] of Object.entries(uniqueTeams)) {
      let timeSpanTotal = new TimeSpan(0);

      for (let entry of v) {
        timeSpanTotal = timeSpanTotal.add(
          this.GetTimeSpanDiff(entry.startTime, entry.endTime, rStart, rEnd)
        );
      }

      this.TotalDuration = this.TotalDuration?.add(timeSpanTotal);

      this.Summaries.push({
        team: k,
        duration: timeSpanTotal,
        entries: v,
      });
    }
  }

  public AdjustStart(start: Date, rStart: Date): Date {
    return start < rStart ? rStart : start;
  }

  public AdjustEnd(end: Date, rEnd: Date): Date {
    return rEnd < end ? rEnd : end;
  }

  public GetTimeSpanDiff(
    start: Date,
    end: Date,
    rStart: Date,
    rEnd: Date
  ): TimeSpan {
    const eStart = this.AdjustStart(start, rStart);
    const eEnd = this.AdjustEnd(end, rEnd);

    const diff = new TimeSpan(eEnd.getTime() - eStart.getTime());

    return diff;
  }
}
