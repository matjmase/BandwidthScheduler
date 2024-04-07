import { HttpClient } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { ILoginCredentials } from '../../models/ILoginCredential';
import { ILoginResponse } from '../../models/ILoginResponse';
import { IRegisterCredentials } from '../../models/IRegisterCredentials';

export class LoginController {
  public RolesHaveChanged = new Subject<void>();
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'login/';
  }

  public GetAllRoles(): Observable<string[]> {
    return this.http.get<string[]>(this._baseUrl + 'roles');
  }

  public Login(creds: ILoginCredentials): Observable<ILoginResponse> {
    return this.http.post<ILoginResponse>(this._baseUrl + 'login', creds).pipe(
      tap({
        next: (val) => this.SaveResponse(val),
      })
    );
  }

  public Register(creds: IRegisterCredentials): Observable<ILoginResponse> {
    return this.http
      .post<ILoginResponse>(this._baseUrl + 'register', creds)
      .pipe(
        tap({
          next: (val) => this.SaveResponse(val),
        })
      );
  }

  public Logout(): void {
    this.RemoveSession();
  }

  private SaveResponse(response: ILoginResponse) {
    localStorage.setItem('Authentication', JSON.stringify(response));
    this.RolesHaveChanged.next();
  }

  private RemoveSession() {
    localStorage.clear();
    this.RolesHaveChanged.next();
  }

  public GetSavedResponse(): ILoginResponse | undefined {
    const strResp = localStorage.getItem('Authentication');

    if (!strResp) {
      return undefined;
    } else {
      const response = <ILoginResponse>JSON.parse(strResp);

      if (new Date(response.validUntil) < new Date()) {
        this.RemoveSession();
        return undefined;
      }

      return response;
    }
  }
}
