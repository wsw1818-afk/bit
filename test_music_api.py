import requests

def test_get_songs():
    url = 'http://api.musicstreaming.com/songs'
    response = requests.get(url)
    assert response.status_code == 200
    assert 'songs' in response.json()

def test_get_users():
    url = 'http://api.musicstreaming.com/users'
    response = requests.get(url)
    assert response.status_code == 200
    assert 'users' in response.json()
