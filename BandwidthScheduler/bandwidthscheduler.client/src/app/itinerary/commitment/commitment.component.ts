import { Component } from '@angular/core';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';
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
export class CommitmentComponent {
  public loading: boolean = false;

  private timeRange: DateTimeRangeSelectorModel | undefined;

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

  constructor(private backend: BackendConnectService) {}

  public SelectedDateRange(range: DateTimeRangeSelectorModel): void {
    this.loading = true;
    this.timeRange = range;
    this.backend.Commitment.GetUserCommitments(range).subscribe({
      next: (commitments) => {
        this.CreateSummaries(commitments, range.start, range.end);
      },
      complete: () => (this.loading = false),
      error: () => (this.loading = false),
    });
  }

  private CreateSummaries(entries: CommitmentEntry[], start: Date, end: Date) {
    const uniqueTeams: { [key: string]: CommitmentEntry[] } = {};

    for (let entry of entries) {
      const teamName = entry.team.name;
      if (teamName in uniqueTeams) {
        uniqueTeams[teamName].push(entry);
      } else {
        uniqueTeams[teamName] = [entry];
      }
    }

    const adjustStart: (s: Date) => Date = (s) => (s < start ? start : s);
    const adjustEnd: (e: Date) => Date = (e) => (e > end ? end : e);

    this.Summaries = [];

    for (let [k, v] of Object.entries(uniqueTeams)) {
      let timeSpanTotal = new TimeSpan(0);

      for (let entry of v) {
        timeSpanTotal = timeSpanTotal.add(
          this.GetTimeSpanDiff(entry.startTime, entry.endTime)
        );
      }

      this.Summaries.push({
        team: k,
        duration: timeSpanTotal,
        entries: v,
      });
    }
  }

  public AdjustStart(start: Date): Date {
    const rStart = this.timeRange!.start;
    return start < rStart ? rStart : start;
  }

  public AdjustEnd(end: Date): Date {
    const rEnd = this.timeRange!.end;
    return rEnd > end ? end : rEnd;
  }

  public GetTimeSpanDiff(start: Date, end: Date): TimeSpan {
    const eStart = this.AdjustStart(start);
    const eEnd = this.AdjustEnd(end);

    const diff = new TimeSpan(eEnd.getTime() - eStart.getTime());

    return diff;
  }
}
