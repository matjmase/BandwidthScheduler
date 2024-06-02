import { Component, ViewChild } from '@angular/core';
import { AddRemoveTeamUserComponent } from './add-remove-team-user/add-remove-team-user.component';
import { EditTeamComponent } from './edit-team/edit-team.component';
import { RemoveTeamComponent } from './remove-team/remove-team.component';

@Component({
  selector: 'app-team',
  templateUrl: './team.component.html',
  styleUrl: './team.component.scss',
})
export class TeamComponent {
  @ViewChild('editMemberControl')
  EditControl!: EditTeamComponent;
  @ViewChild('removeMemberControl')
  RemoveControl!: RemoveTeamComponent;
  @ViewChild('addRemoveMemberControl')
  AddRemoveControl!: AddRemoveTeamUserComponent;

  public TeamsUpdated(): void {
    this.EditControl.TeamCollectionUpdated();
    this.RemoveControl.TeamCollectionUpdated();
    this.AddRemoveControl.TeamCollectionUpdated();
  }
}
