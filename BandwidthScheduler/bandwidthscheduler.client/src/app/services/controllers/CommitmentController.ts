import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';
import { JsonCustom } from '../../models/JsonCustom';
import { ICommitment } from '../../models/db/ICommitment';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';

export class CommitmentController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'commitment/';
  }

  public GetUserCommitments(
    range: DateTimeRangeSelectorModel
  ): Observable<CommitmentEntry[]> {
    const headers = new HttpHeaders()
      .set('start', JsonCustom.stringify(range.start))
      .set('end', JsonCustom.stringify(range.end));
    return this.http
      .get<ICommitment[]>(this._baseUrl + 'user', {
        headers: headers,
      })
      .pipe(map((resp) => resp.map((e) => new CommitmentEntry(e))));
  }

  public GetTeamCommitments(
    range: DateTimeRangeSelectorModel,
    teamId: number
  ): Observable<CommitmentEntry[]> {
    const headers = new HttpHeaders()
      .set('start', JsonCustom.stringify(range.start))
      .set('end', JsonCustom.stringify(range.end))
      .set('teamId', JsonCustom.stringify(teamId));
    return this.http
      .get<ICommitment[]>(this._baseUrl + 'team', {
        headers: headers,
      })
      .pipe(map((resp) => resp.map((e) => new CommitmentEntry(e))));
  }
}
