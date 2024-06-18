import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { JsonCustom } from '../../models/JsonCustom';
import { ICommitment } from '../../models/db/ICommitment';
import { CommitmentEntry } from '../../models/db/CommitmentEntry';
import { IDateRangeSelectorModel } from '../../commonControls/date-range-selector/IDateRangeSelectorModel';

export class CommitmentController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'commitment/';
  }

  public GetUserCommitments(
    range: IDateRangeSelectorModel
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
    range: IDateRangeSelectorModel,
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
